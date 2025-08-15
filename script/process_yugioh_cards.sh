#!/bin/bash
SCRIPT_DIR=$(dirname "$0")
cd "$SCRIPT_DIR/.." || exit 1
TMP_DIR="tmp"
rm -rf "$TMP_DIR"
mkdir -p "$TMP_DIR"
echo "正在安装所需依赖..."
if [ -n "$GITHUB_ACTIONS" ]; then
    echo "在GitHub Actions环境中运行,使用python3和pip3..."
    if command -v python3 &> /dev/null; then
        echo "使用python3 -m pip安装依赖..."
        python3 -m pip install -r requirements.txt || {
            echo "尝试安装pip..."
            dnf install -y python3-pip && python3 -m pip install -r requirements.txt
        }
    else
        echo "错误: 无法找到python3命令,无法安装依赖"
        exit 1
    fi
else
    if command -v pip &> /dev/null; then
        echo "使用pip安装依赖..."
        pip install -r requirements.txt
    elif command -v pip3 &> /dev/null; then
        echo "使用pip3安装依赖..."
        pip3 install -r requirements.txt
    else
        echo "警告: 未找到pip或pip3,尝试使用python -m pip安装..."
        if command -v python &> /dev/null; then
            python -m pip install -r requirements.txt
        elif command -v python3 &> /dev/null; then
            python3 -m pip install -r requirements.txt
        else
            echo "错误: 无法找到pip、python或python3命令,无法安装依赖"
            exit 1
        fi
    fi
fi
echo "正在运行update_banlist.py更新禁限卡表..."
python update_banlist.py
if [ $? -ne 0 ]; then
    echo "update_banlist.py执行失败,请检查错误信息"
    exit 1
fi
if [ -z "$GITHUB_ACTIONS" ]; then
    current_fd_limit=$(ulimit -n)
    if [ "$current_fd_limit" -lt 4096 ]; then
        echo "增加文件描述符限制到4096（原限制：$current_fd_limit）"
        ulimit -n 4096 || echo "警告: 无法增加文件描述符限制,可能需要root权限"
    fi
    current_proc_limit=$(ulimit -u)
    if [ "$current_proc_limit" -lt 4096 ]; then
        echo "增加进程数限制到4096（原限制：$current_proc_limit）"
        ulimit -u 4096 || echo "警告: 无法增加进程数限制,可能需要root权限"
    fi
fi
echo "正在下载ygocdb卡片数据..."
wget -q https://ygocdb.com/api/v0/cards.zip -O "$TMP_DIR/ygocdb_cards.zip"
unzip -q -o "$TMP_DIR/ygocdb_cards.zip" -d "$TMP_DIR"
jq . "$TMP_DIR/cards.json" > "$TMP_DIR/ygocdb_cards.json"
rm "$TMP_DIR/cards.json" "$TMP_DIR/ygocdb_cards.zip"
echo "正在下载ygoprodeck卡片数据..."
curl -s https://db.ygoprodeck.com/api/v7/cardinfo.php | jq . > "$TMP_DIR/ygoprodeck_cardinfo.json"
mkdir -p "$TMP_DIR/figure"
THREAD_NUM=200
echo "正在提取卡片图片URL..."
jq -r '.data[].card_images[].image_url_cropped' "$TMP_DIR/ygoprodeck_cardinfo.json" > "$TMP_DIR/image_urls.txt"
TOTAL=$(wc -l < "$TMP_DIR/image_urls.txt")
if [ $TOTAL -eq 0 ]; then
    echo "错误: 没有找到图片URL,请检查JSON文件是否有效"
    exit 1
fi
cat > "$TMP_DIR/download_worker.sh" << 'EOF'
#!/bin/bash
url="$1"
filename=$(basename "$url")
output_path="tmp/figure/$filename"
final_png_path="${output_path%.jpg}.png"
if [[ ! -f "$final_png_path" ]]; then
    temp_path="${output_path}.tmp"
    wget -q --timeout=30 --tries=3 --retry-connrefused --waitretry=1 "$url" -O "$temp_path"
    if [ $? -ne 0 ]; then
        echo "下载失败: $url"
        rm -f "$temp_path" 2>/dev/null
        exit 1
    fi
    if [ ! -s "$temp_path" ]; then
        echo "删除空文件: $temp_path"
        rm -f "$temp_path"
        exit 1
    fi
    if ! magick identify "$temp_path" &>/dev/null; then
        echo "删除损坏的图片: $temp_path"
        rm -f "$temp_path"
        exit 1
    fi
    if magick "$temp_path" "$final_png_path"; then
        if ! magick identify "$final_png_path" &>/dev/null || [ ! -s "$final_png_path" ]; then
            echo "PNG转换后无效,删除: $final_png_path"
            rm -f "$final_png_path"
            rm -f "$temp_path"
            exit 1
        fi
        # 转换成功,删除临时文件
        rm -f "$temp_path"
        # echo "成功下载并转换为PNG: $final_png_path"
    else
        echo "转换PNG失败: $temp_path"
        rm -f "$temp_path"
        rm -f "$final_png_path" 2>/dev/null
        exit 1
    fi
else
    if ! magick identify "$final_png_path" &>/dev/null || [ ! -s "$final_png_path" ]; then
        echo "发现无效的PNG文件,重新下载: $url"
        rm -f "$final_png_path"
        # 递归调用自身以重新下载
        $0 "$url"
    else
        echo "PNG文件已存在且有效: $final_png_path"
    fi
fi
EOF
chmod +x "$TMP_DIR/download_worker.sh"
echo "开始下载卡片图片,使用 $THREAD_NUM 个并行进程..."
timeout 3600 bash -c "cat \"$TMP_DIR/image_urls.txt\" | xargs -P $THREAD_NUM -I {} ./\"$TMP_DIR/download_worker.sh\" {}"
download_status=$?
if [ $download_status -eq 124 ]; then
    echo "警告: 下载操作超时（1小时）,部分图片可能未下载完成"
elif [ $download_status -ne 0 ]; then
    echo "警告: 下载过程中出现错误,退出代码 $download_status"
fi
DOWNLOADED=$(ls -1 "$TMP_DIR/figure" 2>/dev/null | wc -l)
echo "图片下载完成！成功下载 $DOWNLOADED 张图片到 $TMP_DIR/figure 目录"
if [ $DOWNLOADED -eq 0 ]; then
    echo "警告: 没有成功下载任何图片,请检查网络连接或URL列表"
elif [ $DOWNLOADED -lt $TOTAL ]; then
    echo "提示: 部分图片可能未能下载,成功率: $(($DOWNLOADED * 100 / $TOTAL))%"
fi
rm -f "$TMP_DIR/image_urls.txt" "$TMP_DIR/download_worker.sh"
echo "开始处理卡片数据..."
TYPELINE_CONF="res/typeline.conf"
if [ ! -f "$TYPELINE_CONF" ]; then
    echo "错误: 找不到typeline配置文件: $TYPELINE_CONF"
    exit 1
fi
chmod +x process_yugioh_cards.py
python3 process_yugioh_cards.py
if [ $? -ne 0 ]; then
    echo "错误: 卡片数据处理失败"
    exit 1
fi
echo "卡片处理完成！数据已保存到 $TMP_DIR/cards.json"
echo "正在对cards.json按ID升序排序..."
if command -v jq &> /dev/null; then
    cp "$TMP_DIR/cards.json" "$TMP_DIR/cards_unsorted.json"
    cat "$TMP_DIR/cards_unsorted.json" | jq -S 'to_entries | sort_by(.key | tonumber) | from_entries' > "$TMP_DIR/cards.json"
    rm -f "$TMP_DIR/cards_unsorted.json"
    echo "排序完成！"
else
    echo "警告: 未找到jq工具,跳过排序步骤"
fi
rm -f "$TMP_DIR/ygocdb_cards.json" "$TMP_DIR/ygoprodeck_cardinfo.json"
echo "正在执行最终清理检查..."
if [ -d "$TMP_DIR/figure" ]; then
    jpg_count=$(find "$TMP_DIR/figure" -name "*.jpg" | wc -l)
    if [ $jpg_count -gt 0 ]; then
        echo "警告：仍有 $jpg_count 个JPG文件,这些文件将被删除"
        find "$TMP_DIR/figure" -name "*.jpg" -delete
    fi
    find "$TMP_DIR/figure" -name "*.tmp" -delete
    corrupted_count=0
    for file in "$TMP_DIR/figure"/*.png; do
        if [ -f "$file" ]; then
            if ! magick identify "$file" &>/dev/null || [ ! -s "$file" ]; then
                echo "删除无效的PNG图片文件: $file"
                rm -f "$file"
                ((corrupted_count++))
            fi
        fi
    done
    if [ $corrupted_count -gt 0 ]; then
        echo "最终清理中删除了 $corrupted_count 个无效的PNG文件"
    fi
    find "$TMP_DIR/figure" -type f ! -name "*.png" -delete
    final_png_count=$(find "$TMP_DIR/figure" -name "*.png" | wc -l)
    echo "清理完成！$TMP_DIR/figure 目录中有 $final_png_count 个有效的PNG文件"
else
    echo "警告: $TMP_DIR/figure 目录不存在,跳过最终清理"
fi
echo "所有操作已完成！"
exit 0
