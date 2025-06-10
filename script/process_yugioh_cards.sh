#!/bin/zsh
SCRIPT_DIR=$(dirname "$0")
cd "$SCRIPT_DIR/.." || exit 1
TMP_DIR="tmp"
rm -rf "$TMP_DIR"
mkdir -p "$TMP_DIR"
current_fd_limit=$(ulimit -n)
if [ "$current_fd_limit" -lt 4096 ]; then
    echo "增加文件描述符限制到4096（原限制：$current_fd_limit）"
    ulimit -n 4096 || echo "警告: 无法增加文件描述符限制，可能需要root权限"
fi
current_proc_limit=$(ulimit -u)
if [ "$current_proc_limit" -lt 4096 ]; then
    echo "增加进程数限制到4096（原限制：$current_proc_limit）"
    ulimit -u 4096 || echo "警告: 无法增加进程数限制，可能需要root权限"
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
    echo "错误: 没有找到图片URL，请检查JSON文件是否有效"
    exit 1
fi
cat > "$TMP_DIR/download_worker.sh" << 'EOF'
#!/bin/zsh
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
    if ! identify "$temp_path" &>/dev/null; then
        echo "删除损坏的图片: $temp_path"
        rm -f "$temp_path"
        exit 1
    fi
    if convert "$temp_path" "$final_png_path"; then
        if ! identify "$final_png_path" &>/dev/null || [ ! -s "$final_png_path" ]; then
            echo "PNG转换后无效，删除: $final_png_path"
            rm -f "$final_png_path"
            rm -f "$temp_path"
            exit 1
        fi
        # 转换成功，删除临时文件
        rm -f "$temp_path"
        echo "成功下载并转换为PNG: $final_png_path"
    else
        echo "转换PNG失败: $temp_path"
        rm -f "$temp_path"
        rm -f "$final_png_path" 2>/dev/null
        exit 1
    fi
else
    if ! identify "$final_png_path" &>/dev/null || [ ! -s "$final_png_path" ]; then
        echo "发现无效的PNG文件，重新下载: $url"
        rm -f "$final_png_path"
        # 递归调用自身以重新下载
        $0 "$url"
    else
        echo "PNG文件已存在且有效: $final_png_path"
    fi
fi
EOF
chmod +x "$TMP_DIR/download_worker.sh"
echo "开始下载卡片图片，使用 $THREAD_NUM 个并行进程..."
timeout 3600 bash -c "cat \"$TMP_DIR/image_urls.txt\" | xargs -P $THREAD_NUM -I {} ./\"$TMP_DIR/download_worker.sh\" {}"
download_status=$?
if [ $download_status -eq 124 ]; then
    echo "警告: 下载操作超时（1小时），部分图片可能未下载完成"
elif [ $download_status -ne 0 ]; then
    echo "警告: 下载过程中出现错误，退出代码 $download_status"
fi
DOWNLOADED=$(ls -1 "$TMP_DIR/figure" 2>/dev/null | wc -l)
echo "图片下载完成！成功下载 $DOWNLOADED 张图片到 $TMP_DIR/figure 目录"
if [ $DOWNLOADED -eq 0 ]; then
    echo "警告: 没有成功下载任何图片，请检查网络连接或URL列表"
elif [ $DOWNLOADED -lt $TOTAL ]; then
    echo "提示: 部分图片可能未能下载，成功率: $(($DOWNLOADED * 100 / $TOTAL))%"
fi
rm -f "$TMP_DIR/image_urls.txt"
rm -f "$TMP_DIR/download_worker.sh"
echo "开始处理卡片数据..."
TYPELINE_CONF="res/typeline.conf"
if [ ! -f "$TYPELINE_CONF" ]; then
    echo "错误: 找不到typeline配置文件: $TYPELINE_CONF"
    exit 1
fi
python3 - << 'EOF'
import json
import sys
from pathlib import Path
project_root = Path('.').absolute()
tmp_dir = project_root / "tmp"
typeline_conf_path = project_root / "res" / "typeline.conf"
typeline_dict = {}
try:
    with open(typeline_conf_path, 'r', encoding='utf-8') as f:
        for line in f:
            line = line.strip()
            if not line or line.startswith('//'):
                continue
            if '=' in line:
                key, value = line.split('=', 1)
                typeline_dict[key.strip()] = value.strip()
    print(f"成功加载typeline配置，包含{len(typeline_dict)}个翻译项")
except Exception as e:
    print(f"加载typeline配置文件时出错: {e}")
    sys.exit(1)
prodeck_file = tmp_dir / "ygoprodeck_cardinfo.json"
try:
    with open(prodeck_file, 'r', encoding='utf-8') as f:
        prodeck_data = json.load(f)
    print(f"成功加载ygoprodeck_cardinfo.json，包含{len(prodeck_data.get('data', []))}张卡")
except Exception as e:
    print(f"加载ygoprodeck_cardinfo.json时出错: {e}")
    sys.exit(1)
cdb_file = tmp_dir / "ygocdb_cards.json"
try:
    with open(cdb_file, 'r', encoding='utf-8') as f:
        cdb_data = json.load(f)
    print(f"成功加载ygocdb_cards.json，包含{len(cdb_data)}条记录")
except Exception as e:
    print(f"加载ygocdb_cards.json时出错: {e}")
    sys.exit(1)
id_to_cn_name = {}
id_to_description = {}
id_to_pendulum_description = {}
for card_id_str, card_info in cdb_data.items():
    card_id = card_info.get('id')
    cn_name = card_info.get('cn_name')
    if 'text' in card_info and isinstance(card_info['text'], dict):
        text_info = card_info['text']
        desc = text_info.get('desc', '')
        pdesc = text_info.get('pdesc', '')
        if card_id:
            if desc:
                id_to_description[card_id] = desc.replace('\r\n', '\n')
            if pdesc:
                id_to_pendulum_description[card_id] = pdesc.replace('\r\n', '\n')
    if card_id and cn_name:
        id_to_cn_name[card_id] = cn_name
id_to_scale = {}
id_to_attribute = {}
id_to_atk = {}
id_to_def = {}
id_to_level = {}
id_to_linkval = {}
id_to_linkmarkers = {}
for card in prodeck_data.get('data', []):
    card_id = card.get('id')
    if card_id and 'scale' in card:
        id_to_scale[card_id] = card.get('scale')
    if card_id and 'attribute' in card:
        id_to_attribute[card_id] = card.get('attribute', '').lower()
    if card_id and 'atk' in card:
        id_to_atk[card_id] = card.get('atk')
    if card_id and 'def' in card:
        id_to_def[card_id] = card.get('def')
    if card_id and 'level' in card:
        id_to_level[card_id] = card.get('level')
    if card_id and 'linkval' in card:
        id_to_linkval[card_id] = card.get('linkval')
    if card_id and 'linkmarkers' in card:
        linkmarkers = card.get('linkmarkers', [])
        id_to_linkmarkers[card_id] = [marker.lower() for marker in linkmarkers]
result = {}
for card in prodeck_data.get('data', []):
    card_id = card.get('id')
    if card_id:
        cn_name = id_to_cn_name.get(card_id)
        if cn_name:
            card_type = None
            human_readable_type = card.get('humanReadableCardType', '').lower()
            if 'monster' in human_readable_type:
                card_type = 'monster'
            elif 'spell' in human_readable_type:
                card_type = 'spell'
            elif 'trap' in human_readable_type:
                card_type = 'trap'
            result[card_id] = {
                "name": cn_name,
                "id": card_id
            }
            if card_id in id_to_description:
                result[card_id]["description"] = id_to_description[card_id]
            if card_type:
                result[card_id]["cardtype"] = card_type
                if card_type == 'monster':
                    if card_id in id_to_attribute:
                        result[card_id]["attribute"] = id_to_attribute[card_id]
                    if card_id in id_to_atk:
                        result[card_id]["atk"] = id_to_atk[card_id]
                    if card_id in id_to_def:
                        result[card_id]["def"] = id_to_def[card_id]
                    if card_id in id_to_level:
                        result[card_id]["level"] = id_to_level[card_id]
                    if 'frameType' in card:
                        frame_type = card.get('frameType')
                        frame_type = frame_type.replace('_', '-')
                        result[card_id]["frameType"] = frame_type
                        if 'pendulum' in frame_type:
                            if card_id in id_to_pendulum_description:
                                result[card_id]["pendulum-description"] = id_to_pendulum_description[card_id]
                            if card_id in id_to_scale:
                                result[card_id]["scale"] = id_to_scale[card_id]
                        if frame_type == 'link' or 'link' in frame_type:
                            if card_id in id_to_linkval:
                                result[card_id]["linkval"] = id_to_linkval[card_id]
                            if card_id in id_to_linkmarkers:
                                result[card_id]["linkmarkers"] = id_to_linkmarkers[card_id]
                    if 'typeline' in card:
                        typeline = card.get('typeline', [])
                        if typeline:
                            translated_typeline = []
                            for type_item in typeline:
                                if type_item in typeline_dict:
                                    translated_typeline.append(typeline_dict[type_item])
                                else:
                                    translated_typeline.append(type_item)
                            if translated_typeline:
                                result[card_id]["typeline"] = f"【{' / '.join(translated_typeline)}】"
                
                if card_type == 'spell' or card_type == 'trap':
                    result[card_id]["frameType"] = card_type
                    result[card_id]["attribute"] = card_type
                    if 'race' in card:
                        race = card.get('race', '')
                        if race:
                            result[card_id]["race"] = race.lower()
            if 'card_images' in card and card['card_images'] and 'image_url_cropped' in card['card_images'][0]:
                image_url = card['card_images'][0]['image_url_cropped']
                image_filename = image_url.split('/')[-1]
                result[card_id]["cardimage"] = image_filename
        else:
            print(f"卡片ID {card_id} 没有找到对应的中文名称，跳过该卡片")
print(f"处理完成，共生成{len(result)}张卡的数据")
output_file = tmp_dir / "cards.json"
try:
    other_json_path = project_root / "res" / "other.json"
    other_data = {}
    if other_json_path.exists():
        try:
            with open(other_json_path, 'r', encoding='utf-8') as f:
                content = f.read().strip()
                if content and content != '{}':
                    other_data = json.loads(content)
                    print(f"成功加载other.json，合并{len(other_data)}条记录")
        except Exception as e:
            print(f"读取other.json时出错: {e}，将跳过合并")
    if other_data:
        for card_id, card_info in other_data.items():
            if card_id in result:
                result[card_id].update(card_info)
            else:
                result[card_id] = card_info
        print(f"合并other.json后，共有{len(result)}张卡的数据")
    
    with open(output_file, 'w', encoding='utf-8') as f:
        json.dump(result, f, ensure_ascii=False, indent=2)
    print(f"成功将结果保存到 {output_file}")
except Exception as e:
    print(f"保存结果到 {output_file} 时出错: {e}")
    sys.exit(1)
EOF
if [ $? -ne 0 ]; then
    echo "错误: 卡片数据处理失败"
    exit 1
fi
echo "卡片处理完成！数据已保存到 $TMP_DIR/cards.json"
echo "正在执行最终清理检查..."
if [ -d "$TMP_DIR/figure" ]; then
    jpg_count=$(find "$TMP_DIR/figure" -name "*.jpg" | wc -l)
    if [ $jpg_count -gt 0 ]; then
        echo "警告：仍有 $jpg_count 个JPG文件，这些文件将被删除"
        find "$TMP_DIR/figure" -name "*.jpg" -delete
    fi
    find "$TMP_DIR/figure" -name "*.tmp" -delete
    corrupted_count=0
    for file in "$TMP_DIR/figure"/*.png; do
        if [ -f "$file" ]; then
            if ! identify "$file" &>/dev/null || [ ! -s "$file" ]; then
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
    echo "警告: $TMP_DIR/figure 目录不存在，跳过最终清理"
fi
echo "所有操作已完成！"
exit 0