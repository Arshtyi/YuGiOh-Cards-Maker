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
mv "$TMP_DIR/cards.json" "$TMP_DIR/ygocdb_cards.json"
rm "$TMP_DIR/ygocdb_cards.zip"
echo "正在下载ygoprodeck卡片数据..."
curl -s https://db.ygoprodeck.com/api/v7/cardinfo.php | jq . > "$TMP_DIR/ygoprodeck_cardinfo.json"
mkdir -p "$TMP_DIR/figure"
THREAD_NUM=500
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
if [[ ! -f "tmp/figure/$filename" ]]; then
    wget -q --timeout=30 --tries=3 --retry-connrefused --waitretry=1 "$url" -O "tmp/figure/$filename" || echo "下载失败: $url"
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
# 获取项目根目录和临时目录
project_root = Path('.').absolute()
tmp_dir = project_root / "tmp"
typeline_conf_path = project_root / "res" / "typeline.conf"

# 加载typeline配置
typeline_dict = {}
try:
    with open(typeline_conf_path, 'r', encoding='utf-8') as f:
        for line in f:
            line = line.strip()
            # 跳过注释和空行
            if not line or line.startswith('//'):
                continue
            if '=' in line:
                key, value = line.split('=', 1)
                typeline_dict[key.strip()] = value.strip()
    print(f"成功加载typeline配置，包含{len(typeline_dict)}个翻译项")
except Exception as e:
    print(f"加载typeline配置文件时出错: {e}")
    sys.exit(1)

# 加载ygoprodeck_cardinfo.json
prodeck_file = tmp_dir / "ygoprodeck_cardinfo.json"
try:
    with open(prodeck_file, 'r', encoding='utf-8') as f:
        prodeck_data = json.load(f)
    print(f"成功加载ygoprodeck_cardinfo.json，包含{len(prodeck_data.get('data', []))}张卡")
except Exception as e:
    print(f"加载ygoprodeck_cardinfo.json时出错: {e}")
    sys.exit(1)

# 加载ygocdb_cards.json
cdb_file = tmp_dir / "ygocdb_cards.json"
try:
    with open(cdb_file, 'r', encoding='utf-8') as f:
        cdb_data = json.load(f)
    print(f"成功加载ygocdb_cards.json，包含{len(cdb_data)}条记录")
except Exception as e:
    print(f"加载ygocdb_cards.json时出错: {e}")
    sys.exit(1)

# 创建ID到中文名称的映射以及卡片描述信息的映射
id_to_cn_name = {}
id_to_description = {}
id_to_pendulum_description = {}

for card_id_str, card_info in cdb_data.items():
    card_id = card_info.get('id')
    cn_name = card_info.get('cn_name')
    
    # 获取描述信息
    if 'text' in card_info and isinstance(card_info['text'], dict):
        text_info = card_info['text']
        desc = text_info.get('desc', '')
        pdesc = text_info.get('pdesc', '')
        
        if card_id:
            if desc:
                # 将 Windows 风格的换行符 \r\n 转换为 Linux 风格的 \n
                id_to_description[card_id] = desc.replace('\r\n', '\n')
            if pdesc:
                # 将 Windows 风格的换行符 \r\n 转换为 Linux 风格的 \n
                id_to_pendulum_description[card_id] = pdesc.replace('\r\n', '\n')
    
    if card_id and cn_name:
        id_to_cn_name[card_id] = cn_name

print(f"从ygocdb_cards.json中提取了{len(id_to_cn_name)}个ID到中文名称的映射")
print(f"从ygocdb_cards.json中提取了{len(id_to_description)}个卡片描述")
print(f"从ygocdb_cards.json中提取了{len(id_to_pendulum_description)}个灵摆卡描述")

# 创建ID到灵摆刻度的映射
id_to_scale = {}
# 创建ID到属性的映射
id_to_attribute = {}
# 创建ID到攻击力、防御力和等级的映射
id_to_atk = {}
id_to_def = {}
id_to_level = {}

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

print(f"从ygoprodeck_cardinfo.json中提取了{len(id_to_scale)}个灵摆刻度")
print(f"从ygoprodeck_cardinfo.json中提取了{len(id_to_attribute)}个怪兽属性")
print(f"从ygoprodeck_cardinfo.json中提取了{len(id_to_atk)}个怪兽攻击力")
print(f"从ygoprodeck_cardinfo.json中提取了{len(id_to_def)}个怪兽防御力")
print(f"从ygoprodeck_cardinfo.json中提取了{len(id_to_level)}个怪兽等级")

# 创建结果数据结构
result = {}

# 遍历ygoprodeck中的每一张卡
for card in prodeck_data.get('data', []):
    card_id = card.get('id')
    if card_id:
        # 查找对应的中文名称
        cn_name = id_to_cn_name.get(card_id)
        if cn_name:
            # 获取humanReadableCardType并确定cardtype
            card_type = None
            human_readable_type = card.get('humanReadableCardType', '').lower()
            if 'monster' in human_readable_type:
                card_type = 'monster'
            elif 'spell' in human_readable_type:
                card_type = 'spell'
            elif 'trap' in human_readable_type:
                card_type = 'trap'
            
            # 将中文名称、ID和cardtype保存在对象中
            result[card_id] = {
                "name": cn_name,
                "id": card_id
            }
            
            # 添加描述信息
            if card_id in id_to_description:
                result[card_id]["description"] = id_to_description[card_id]
            
            # 如果有cardtype，添加到结果中
            if card_type:
                result[card_id]["cardtype"] = card_type
                
                # 对于monster类型的卡片，处理typeline和frameType
                if card_type == 'monster':
                    # 添加attribute属性字段
                    if card_id in id_to_attribute:
                        result[card_id]["attribute"] = id_to_attribute[card_id]
                    
                    # 添加atk、def和level字段
                    if card_id in id_to_atk:
                        result[card_id]["atk"] = id_to_atk[card_id]
                    if card_id in id_to_def:
                        result[card_id]["def"] = id_to_def[card_id]
                    if card_id in id_to_level:
                        result[card_id]["level"] = id_to_level[card_id]
                        
                    # 添加frameType字段
                    if 'frameType' in card:
                        frame_type = card.get('frameType')
                        # 将下划线替换为连字符，例如将fusion_pendulum替换为fusion-pendulum
                        frame_type = frame_type.replace('_', '-')
                        result[card_id]["frameType"] = frame_type
                        
                        # 如果是灵摆卡，添加灵摆描述和刻度
                        if 'pendulum' in frame_type:
                            if card_id in id_to_pendulum_description:
                                result[card_id]["pendulum-description"] = id_to_pendulum_description[card_id]
                            if card_id in id_to_scale:
                                result[card_id]["scale"] = id_to_scale[card_id]
                        
                    # 处理typeline
                    if 'typeline' in card:
                        typeline = card.get('typeline', [])
                        if typeline:
                            # 翻译typeline
                            translated_typeline = []
                            for type_item in typeline:
                                if type_item in typeline_dict:
                                    translated_typeline.append(typeline_dict[type_item])
                                else:
                                    # 如果没有找到翻译，使用原始值
                                    translated_typeline.append(type_item)
                            
                            # 将翻译后的typeline加入结果，格式为【A / B / C】（中括号两边没有空格，斜杠两边有空格）
                            if translated_typeline:
                                result[card_id]["typeline"] = f"【{' / '.join(translated_typeline)}】"
                
                # 对于spell和trap类型的卡片，添加race字段（小写）和frameType
                if card_type == 'spell' or card_type == 'trap':
                    # 添加frameType字段为固定值
                    result[card_id]["frameType"] = card_type
                    
                    # 添加attribute属性，spell和trap卡的attribute就是它们自身的类型
                    result[card_id]["attribute"] = card_type
                    
                    # 添加race字段（如果存在）
                    if 'race' in card:
                        race = card.get('race', '')
                        if race:
                            result[card_id]["race"] = race.lower()
            
            # 添加cardimage字段，从card_images中提取image_url_cropped项的文件名部分
            if 'card_images' in card and card['card_images'] and 'image_url_cropped' in card['card_images'][0]:
                image_url = card['card_images'][0]['image_url_cropped']
                # 只保留URL的文件名部分（包括后缀）
                image_filename = image_url.split('/')[-1]
                result[card_id]["cardimage"] = image_filename
        else:
            # 如果没有中文名称，跳过这张卡
            print(f"卡片ID {card_id} 没有找到对应的中文名称，跳过该卡片")

print(f"处理完成，共生成{len(result)}张卡的数据")

# 保存结果到新的JSON文件
output_file = tmp_dir / "cards.json"
try:
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
rm -f "$TMP_DIR/ygocdb_cards.json" "$TMP_DIR/ygoprodeck_cardinfo.json"
echo "卡片处理完成！数据已保存到 $TMP_DIR/cards.json"
echo "所有操作已完成！"
exit 0
