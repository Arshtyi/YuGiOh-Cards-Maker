#!/bin/bash
SCRIPT_DIR=$(dirname "$0")
cd "$SCRIPT_DIR/.." || exit 1
TMP_DIR="tmp"
rm -rf "$TMP_DIR"
mkdir -p "$TMP_DIR"
echo "准备下载并校验外部资源: typeline.conf, token.json, forbidden_and_limited_list.tar.xz, yugioh-card-template.tar.xz..."
download_file() {
    url="$1"
    out="$2"
    if command -v curl &> /dev/null; then
        curl -sSfL "$url" -o "$out"
        return $?
    elif command -v wget &> /dev/null; then
        wget -q "$url" -O "$out"
        return $?
    else
        echo "错误: 未找到 curl 或 wget, 无法下载 $url"
        return 2
    fi
}
verify_sha256() {
    shafile="$1"
    targetfile="$2"
    if command -v sha256sum &> /dev/null; then
        expected=$(awk '{print $1}' "$shafile" | head -n1)
        actual=$(sha256sum "$targetfile" | awk '{print $1}')
        if [ "$expected" != "$actual" ]; then
            echo "校验失败: $targetfile (期望 $expected, 实际 $actual)"
            return 1
        fi
        return 0
    elif command -v shasum &> /dev/null; then
        expected=$(awk '{print $1}' "$shafile" | head -n1)
        actual=$(shasum -a 256 "$targetfile" | awk '{print $1}')
        if [ "$expected" != "$actual" ]; then
            echo "校验失败: $targetfile (期望 $expected, 实际 $actual)"
            return 1
        fi
        return 0
    else
        echo "警告: 未找到 sha256sum 或 shasum, 跳过校验"
        return 0
    fi
}
RES_DIR="res"
mkdir -p "$RES_DIR"
TYPELINE_URL="https://github.com/Arshtyi/Translations-Of-YuGiOh-Cards-Type/releases/download/latest/typeline.conf"
TYPELINE_SHA_URL="https://github.com/Arshtyi/Translations-Of-YuGiOh-Cards-Type/releases/download/latest/typeline.conf.sha256"
TYPELINE_PATH="$RES_DIR/typeline.conf"
TYPELINE_SHA_PATH="$TMP_DIR/typeline.conf.sha256"
echo "下载 typeline.conf 到 $TYPELINE_PATH"
download_file "$TYPELINE_URL" "$TYPELINE_PATH"
if [ $? -ne 0 ]; then
    echo "错误: 下载 typeline.conf 失败"
    exit 1
fi
download_file "$TYPELINE_SHA_URL" "$TYPELINE_SHA_PATH"
if [ $? -ne 0 ]; then
    echo "警告: 无法下载 typeline.conf.sha256, 将跳过校验"
else
    verify_sha256 "$TYPELINE_SHA_PATH" "$TYPELINE_PATH"
    if [ $? -ne 0 ]; then
        echo "错误: typeline.conf 校验失败"
        exit 1
    fi
fi
rm -f "$TYPELINE_SHA_PATH"
TOKEN_URL="https://github.com/Arshtyi/YuGiOh-Tokens/releases/download/latest/token.json"
TOKEN_SHA_URL="https://github.com/Arshtyi/YuGiOh-Tokens/releases/download/latest/token.json.sha256"
TOKEN_PATH="$RES_DIR/token.json"
TOKEN_SHA_PATH="$TMP_DIR/token.json.sha256"
echo "下载 token.json 到 $TOKEN_PATH"
download_file "$TOKEN_URL" "$TOKEN_PATH"
if [ $? -ne 0 ]; then
    echo "错误: 下载 token.json 失败"
    exit 1
fi
download_file "$TOKEN_SHA_URL" "$TOKEN_SHA_PATH"
if [ $? -ne 0 ]; then
    echo "警告: 无法下载 token.json.sha256, 将跳过校验"
else
    verify_sha256 "$TOKEN_SHA_PATH" "$TOKEN_PATH"
    if [ $? -ne 0 ]; then
        echo "错误: token.json 校验失败"
        exit 1
    fi
fi
rm -f "$TOKEN_SHA_PATH"
LIMIT_URL="https://github.com/Arshtyi/YuGiOh-Forbidden-And-Limited-List/releases/download/latest/forbidden_and_limited_list.tar.xz"
LIMIT_SHA_URL="https://github.com/Arshtyi/YuGiOh-Forbidden-And-Limited-List/releases/download/latest/forbidden_and_limited_list.tar.xz.sha256"
LIMIT_TAR="$TMP_DIR/forbidden_and_limited_list.tar.xz"
LIMIT_SHA="$TMP_DIR/forbidden_and_limited_list.tar.xz.sha256"
LIMIT_DIR="$RES_DIR/limit"
mkdir -p "$LIMIT_DIR"
echo "下载 forbidden_and_limited_list.tar.xz 到 $LIMIT_TAR"
download_file "$LIMIT_URL" "$LIMIT_TAR"
if [ $? -ne 0 ]; then
    echo "错误: 下载 forbidden_and_limited_list.tar.xz 失败"
    exit 1
fi
download_file "$LIMIT_SHA_URL" "$LIMIT_SHA"
if [ $? -ne 0 ]; then
    echo "警告: 无法下载 forbidden_and_limited_list.tar.xz.sha256, 将跳过校验"
else
    verify_sha256 "$LIMIT_SHA" "$LIMIT_TAR"
    if [ $? -ne 0 ]; then
        echo "错误: forbidden_and_limited_list.tar.xz 校验失败"
        exit 1
    fi
fi
echo "解压 $LIMIT_TAR 到 $LIMIT_DIR"
tar -xJf "$LIMIT_TAR" -C "$LIMIT_DIR"
if [ $? -ne 0 ]; then
    echo "错误: 解压 forbidden_and_limited_list.tar.xz 失败"
    exit 1
fi
rm -f "$LIMIT_TAR" "$LIMIT_SHA"
TEMPLATE_URL="https://github.com/Arshtyi/Card-Templates-Of-YuGiOh/releases/download/1-11/yugioh-card-template.tar.xz"
TEMPLATE_SHA_URL="https://github.com/Arshtyi/Card-Templates-Of-YuGiOh/releases/download/1-11/yugioh-card-template.tar.xz.sha256"
TEMPLATE_TAR="$TMP_DIR/yugioh-card-template.tar.xz"
TEMPLATE_SHA="$TMP_DIR/yugioh-card-template.tar.xz.sha256"
ASSET_DIR="asset"
mkdir -p "$ASSET_DIR"
echo "下载 yugioh-card-template.tar.xz 到 $TEMPLATE_TAR"
download_file "$TEMPLATE_URL" "$TEMPLATE_TAR"
if [ $? -ne 0 ]; then
    echo "错误: 下载 yugioh-card-template.tar.xz 失败"
    exit 1
fi
echo "下载 yugioh-card-template.tar.xz.sha256 到 $TEMPLATE_SHA"
download_file "$TEMPLATE_SHA_URL" "$TEMPLATE_SHA"
if [ $? -ne 0 ]; then
    echo "警告: 无法下载 yugioh-card-template.tar.xz.sha256, 将跳过校验"
else
    verify_sha256 "$TEMPLATE_SHA" "$TEMPLATE_TAR"
    if [ $? -ne 0 ]; then
        echo "错误: yugioh-card-template.tar.xz 校验失败"
        exit 1
    fi
fi
echo "解压 $TEMPLATE_TAR 到 $ASSET_DIR"
tar -xJf "$TEMPLATE_TAR" -C "$ASSET_DIR"
if [ $? -ne 0 ]; then
    echo "错误: 解压 yugioh-card-template.tar.xz 到 $ASSET_DIR 失败"
    exit 1
fi
rm -f "$TEMPLATE_TAR" "$TEMPLATE_SHA"
echo "资源下载与校验完成"
echo "正在下载ygocdb卡片数据..."
YGOCDB_ZIP_URL="https://ygocdb.com/api/v0/cards.zip"
YGOCDB_MD5_URL="$YGOCDB_ZIP_URL.md5"
YGOCDB_ZIP="$TMP_DIR/ygocdb_cards.zip"
YGOCDB_MD5="$TMP_DIR/ygocdb_cards.zip.md5"
YGOCDB_EXPECTED_MD5=""
echo "下载 cards.zip 到 $YGOCDB_ZIP"
_restore_proxy_env() {
    for v in HTTP_PROXY HTTPS_PROXY http_proxy https_proxy ALL_PROXY all_proxy; do
        old_val="_OLD_${v}"
        old_set="_OLDSET_${v}"
        if [ "${!old_set}" = "1" ]; then
            export ${v}="${!old_val}"
        else
            unset ${v}
        fi
    done
}
for v in HTTP_PROXY HTTPS_PROXY http_proxy https_proxy ALL_PROXY all_proxy; do
    if [ "${!v+set}" = "set" ]; then
        eval "_OLDSET_${v}=1"
        eval "_OLD_${v}='${!v}'"
    else
        eval "_OLDSET_${v}=0"
        eval "_OLD_${v}=''"
    fi
    unset $v
done
trap '_restore_proxy_env' EXIT
download_file "$YGOCDB_ZIP_URL" "$YGOCDB_ZIP"
if [ $? -ne 0 ]; then
    echo "错误: 下载 cards.zip 失败"
    exit 1
fi
echo "尝试下载 cards.zip 的 MD5 校验文件: $YGOCDB_MD5_URL"
download_file "$YGOCDB_MD5_URL" "$YGOCDB_MD5"
if [ $? -ne 0 ]; then
    echo "警告: 无法下载 cards.zip.md5, 将尝试使用本地工具计算并继续 (不建议)"
else
    expected_md5=$(awk '{print $1}' "$YGOCDB_MD5" | head -n1)
    expected_md5=${expected_md5%\"}
    expected_md5=${expected_md5#\"} # 百鸽的md5sum看起来是人工填写，已经反馈
    if [ -z "$expected_md5" ]; then
        echo "警告: 下载的 MD5 文件为空或格式不识别, 将跳过校验"
    else
        YGOCDB_EXPECTED_MD5="$expected_md5"
        echo "已获取 cards.json 的 MD5: $YGOCDB_EXPECTED_MD5 (将在解压后验证)"
    fi
fi
rm -f "$YGOCDB_MD5"
unzip -q -o "$YGOCDB_ZIP" -d "$TMP_DIR"
if [ -n "$YGOCDB_EXPECTED_MD5" ]; then
    echo "验证 cards.json 的 MD5..."
    if command -v md5sum &> /dev/null; then
        actual_md5=$(md5sum "$TMP_DIR/cards.json" | awk '{print $1}')
    elif command -v md5 &> /dev/null; then
        actual_md5=$(md5 -q "$TMP_DIR/cards.json")
    elif command -v openssl &> /dev/null; then
        actual_md5=$(openssl dgst -md5 "$TMP_DIR/cards.json" | awk '{print $2}')
    else
        echo "错误: 系统上未找到 md5sum/md5/openssl, 无法验证 cards.json"
        rm -f "$YGOCDB_ZIP"
        exit 1
    fi
    if [ "$YGOCDB_EXPECTED_MD5" != "$actual_md5" ]; then
        echo "错误: cards.json MD5 校验失败 (期望: $YGOCDB_EXPECTED_MD5, 实际: $actual_md5)"
        rm -f "$YGOCDB_ZIP" "$TMP_DIR/cards.json"
        exit 1
    fi
    echo "cards.json MD5 校验通过"
fi
jq . "$TMP_DIR/cards.json" > "$TMP_DIR/ygocdb_cards.json"
rm "$TMP_DIR/cards.json" "$YGOCDB_ZIP"
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
