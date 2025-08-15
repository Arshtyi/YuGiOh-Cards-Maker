import json
import sys
import os
from pathlib import Path

def main():
    # 获取项目根目录
    project_root = Path(os.getcwd()).absolute()
    tmp_dir = project_root / "tmp"
    typeline_conf_path = project_root / "res" / "typeline.conf"
    typeline_dict = {}
    untranslated_typelines = 0
    try:
        with open(typeline_conf_path, 'r', encoding='utf-8') as f:
            for line in f:
                line = line.strip()
                if not line or line.startswith('//'):
                    continue
                if '=' in line:
                    key, value = line.split('=', 1)
                    typeline_dict[key.strip()] = value.strip()
        print(f"成功加载typeline配置,包含{len(typeline_dict)}个翻译项")
    except Exception as e:
        print(f"加载typeline配置文件时出错: {e}")
        sys.exit(1)
    prodeck_file = tmp_dir / "ygoprodeck_cardinfo.json"
    try:
        with open(prodeck_file, 'r', encoding='utf-8') as f:
            prodeck_data = json.load(f)
        print(f"成功加载ygoprodeck_cardinfo.json,包含{len(prodeck_data.get('data', []))}张卡")
    except Exception as e:
        print(f"加载ygoprodeck_cardinfo.json时出错: {e}")
        sys.exit(1)
    cdb_file = tmp_dir / "ygocdb_cards.json"
    try:
        with open(cdb_file, 'r', encoding='utf-8') as f:
            cdb_data = json.load(f)
        print(f"成功加载ygocdb_cards.json,包含{len(cdb_data)}条记录")
    except Exception as e:
        print(f"加载ygocdb_cards.json时出错: {e}")
        sys.exit(1)
    id_to_cn_name = {}
    id_to_description = {}
    id_to_pendulum_description = {}
    id_to_type_bracket = {}  # 存储卡片types中括号内的内容
    untranslated_typelines = 0  # 声明全局变量
    for card_id_str, card_info in cdb_data.items():
        card_id = card_info.get('id')
        cn_name = card_info.get('cn_name')
        if 'text' in card_info and isinstance(card_info['text'], dict):
            text_info = card_info['text']
            desc = text_info.get('desc', '')
            pdesc = text_info.get('pdesc', '')
            types = text_info.get('types', '')
            if card_id:
                if desc:
                    id_to_description[card_id] = desc.replace('\r\n', '\n')
                if pdesc:
                    id_to_pendulum_description[card_id] = pdesc.replace('\r\n', '\n')
                if types:
                    # 从types中提取中括号内的内容,例如从"[怪兽|效果] 昆虫/暗\n[★3] 0/0"提取"怪兽|效果"
                    import re
                    bracket_match = re.search(r'\[(.*?)\]', types)
                    if bracket_match:
                        bracket_content = bracket_match.group(1)
                        id_to_type_bracket[card_id] = bracket_content
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
        # 确定卡片的id：优先使用 card_images 中出现在 ygocdb (id_to_cn_name) 的 id，
        # 否则回退到 prodeck 当中的原始 id。
        original_id = card.get('id')
        card_id = None
        # 如果有 card_images，优先检查其中的 id 是否在 ygocdb 中存在
        if 'card_images' in card and card.get('card_images'):
            for img in card.get('card_images', []):
                img_id = img.get('id')
                if img_id and img_id in id_to_cn_name:
                    card_id = img_id
                    break
        # 回退到原始 id
        if card_id is None:
            card_id = original_id
        if card_id:
            cn_name = id_to_cn_name.get(card_id)
            if cn_name:
                # 如果选择的 id 与 prodeck 原始 id 不同，记录一条信息以便排查
                if original_id and original_id != card_id:
                    print(f"Info: 使用 card_images 中的 id {card_id} 替代原始 prodeck id {original_id} 作为该卡的键")
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
                    result[card_id]["cardType"] = card_type
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
                                    result[card_id]["pendulumDescription"] = id_to_pendulum_description[card_id]
                                if card_id in id_to_scale:
                                    result[card_id]["scale"] = id_to_scale[card_id]
                            if frame_type == 'link' or 'link' in frame_type:
                                if card_id in id_to_linkval:
                                    result[card_id]["linkVal"] = id_to_linkval[card_id]
                                if card_id in id_to_linkmarkers:
                                    result[card_id]["linkMarkers"] = id_to_linkmarkers[card_id]
                        if 'typeline' in card:
                            typeline = card.get('typeline', [])
                            if card_id in id_to_type_bracket:
                                bracket_content = id_to_type_bracket[card_id]
                                parts = bracket_content.split('|')
                                if parts and parts[0].strip() == "怪兽" and len(typeline) > 0:
                                    first_type = typeline[0]
                                    if first_type in typeline_dict:
                                        first_type_translated = typeline_dict[first_type]
                                    else:
                                        print(f"错误: ID {card_id} ({cn_name}) 的typeline '{first_type}' 未在typeline.conf中找到对应翻译")
                                        first_type_translated = first_type
                                        untranslated_typelines += 1
                                    remaining_parts = parts[1:]
                                    remaining_parts.reverse()
                                    final_typeline = [first_type_translated]
                                    for part in remaining_parts:
                                        part = part.strip()
                                        final_typeline.append(part)
                                else:
                                    reversed_parts = list(parts)
                                    reversed_parts.reverse()
                                    final_typeline = [part.strip() for part in reversed_parts if part.strip()]
                                if final_typeline:
                                    result[card_id]["typeline"] = f"【{'/'.join(final_typeline)}】"
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
                    image_filename = os.path.splitext(image_filename)[0]
                    result[card_id]["cardImage"] = image_filename
            else:
                print(f"ID {card_id} 没有找到对应的中文名称,跳过该卡片")
    print(f"处理完成,共生成{len(result)}张卡的数据")
    if untranslated_typelines > 0:
        print(f"警告: 有{untranslated_typelines}个typeline未能在typeline.conf中找到对应翻译")
    output_file = tmp_dir / "cards.json"
    try:
        token_json_path = project_root / "res" / "token.json"
        token_data = {}
        if token_json_path.exists():
            try:
                with open(token_json_path, 'r', encoding='utf-8') as f:
                    content = f.read().strip()
                    if content and content != '{}':
                        token_data = json.loads(content)
                        print(f"成功加载token.json,合并{len(token_data)}条记录")
                        print(f"开始下载token.json中的卡片图片...")
                        import urllib.request
                        import subprocess
                        import tempfile
                        download_count = 0
                        skip_count = 0
                        error_count = 0
                        for card_id in token_data.keys():
                            card_info = token_data[card_id]
                            image_id = card_info["cardImage"]
                            image_url = f"https://images.ygoprodeck.com/images/cards_cropped/{image_id}.jpg"
                            final_png_path = tmp_dir / "figure" / f"{card_id}.png"
                            if os.path.exists(final_png_path) and os.path.getsize(final_png_path) > 0:
                                try:
                                    subprocess.run(["magick", "identify", final_png_path], check=True, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
                                    skip_count += 1
                                    continue
                                except subprocess.CalledProcessError:
                                    os.remove(final_png_path)
                            temp_path = None
                            try:
                                with tempfile.NamedTemporaryFile(suffix='.jpg', dir=tmp_dir / "figure", delete=False) as temp_file:
                                    temp_path = temp_file.name
                                # 下载图片
                                urllib.request.urlretrieve(image_url, temp_path)
                                # 检查图片是否有效
                                try:
                                    subprocess.run(["magick", "identify", temp_path], check=True, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
                                except subprocess.CalledProcessError:
                                    print(f"下载的图片无效: {card_id}")
                                    if temp_path and os.path.exists(temp_path):
                                        os.remove(temp_path)
                                    error_count += 1
                                    continue
                                # 转换为PNG
                                try:
                                    subprocess.run(["magick", temp_path, final_png_path], check=True, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
                                    # 检查转换后的PNG是否有效
                                    subprocess.run(["magick", "identify", final_png_path], check=True, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
                                    # 删除临时文件
                                    if temp_path and os.path.exists(temp_path):
                                        os.remove(temp_path)
                                    download_count += 1
                                except subprocess.CalledProcessError:
                                    print(f"转换PNG失败: {card_id}")
                                    if temp_path and os.path.exists(temp_path):
                                        os.remove(temp_path)
                                    if os.path.exists(final_png_path):
                                        os.remove(final_png_path)
                                    error_count += 1
                            except Exception as e:
                                print(f"下载图片失败: {card_id}, 错误: {e}")
                                if temp_path and os.path.exists(temp_path):
                                    os.remove(temp_path)
                                error_count += 1
                        print(f"token.json卡片图片下载完成: 成功{download_count}张, 跳过{skip_count}张, 失败{error_count}张")
            except Exception as e:
                print(f"读取token.json时出错: {e},将跳过合并")

        if token_data:
            print("正在对token.json数据按name属性进行排序,相同name的按id排序...")
            sorted_token_data = {}
            # 创建(card_id, card_info)的列表以便排序
            card_items = list(token_data.items())
            # 先按name排序,如果name相同则按id（数值）排序
            card_items.sort(key=lambda item: (item[1].get("name", ""), int(item[0])))
            # 重建排序后的字典
            sorted_token_data = {card_id: card_info for card_id, card_info in card_items}
            token_data = sorted_token_data
            print(f"排序完成,共排序了{len(token_data)}条记录")
            try:
                with open(token_json_path, 'w', encoding='utf-8') as f:
                    json.dump(token_data, f, ensure_ascii=False, indent=4)
                print(f"已将排序后的数据写回 {token_json_path}")
            except Exception as e:
                print(f"写入排序后的数据到 {token_json_path} 时出错: {e}")
            for card_id, card_info in token_data.items():
                if card_id in result:
                    result[card_id].update(card_info)
                else:
                    result[card_id] = card_info
            print(f"合并token.json后,共有{len(result)}张卡的数据")
        try:
            ocg_limit_path = project_root / "res" / "limit" / "ocg.json"
            tcg_limit_path = project_root / "res" / "limit" / "tcg.json"
            md_limit_path = project_root / "res" / "limit" / "md.json"
            ocg_limit_data = {}
            tcg_limit_data = {}
            md_limit_data = {}
            if ocg_limit_path.exists():
                with open(ocg_limit_path, 'r', encoding='utf-8') as f:
                    ocg_limit_data = json.load(f)
                print(f"成功加载OCG限制列表,包含 {len(ocg_limit_data.get('forbidden', []))} 张禁止卡, {len(ocg_limit_data.get('limited', []))} 张限制卡, {len(ocg_limit_data.get('semi-limited', []))} 张准限制卡")
            if tcg_limit_path.exists():
                with open(tcg_limit_path, 'r', encoding='utf-8') as f:
                    tcg_limit_data = json.load(f)
                print(f"成功加载TCG限制列表,包含 {len(tcg_limit_data.get('forbidden', []))} 张禁止卡, {len(tcg_limit_data.get('limited', []))} 张限制卡, {len(tcg_limit_data.get('semi-limited', []))} 张准限制卡")
            if md_limit_path.exists():
                with open(md_limit_path, 'r', encoding='utf-8') as f:
                    md_limit_data = json.load(f)
                print(f"成功加载MD限制列表,包含 {len(md_limit_data.get('forbidden', []))} 张禁止卡, {len(md_limit_data.get('limited', []))} 张限制卡, {len(md_limit_data.get('semi-limited', []))} 张准限制卡")
            ocg_forbidden_ids = set(ocg_limit_data.get('forbidden', []))
            ocg_limited_ids = set(ocg_limit_data.get('limited', []))
            ocg_semi_limited_ids = set(ocg_limit_data.get('semi-limited', []))
            tcg_forbidden_ids = set(tcg_limit_data.get('forbidden', []))
            tcg_limited_ids = set(tcg_limit_data.get('limited', []))
            tcg_semi_limited_ids = set(tcg_limit_data.get('semi-limited', []))
            md_forbidden_ids = set(md_limit_data.get('forbidden', []))
            md_limited_ids = set(md_limit_data.get('limited', []))
            md_semi_limited_ids = set(md_limit_data.get('semi-limited', []))
            for card_id_str, card_info in result.items():
                card_id = int(card_id_str)
                ocg_limit = None
                if card_id in ocg_forbidden_ids:
                    ocg_limit = "forbidden"
                elif card_id in ocg_limited_ids:
                    ocg_limit = "limited"
                elif card_id in ocg_semi_limited_ids:
                    ocg_limit = "semi-limited"
                tcg_limit = None
                if card_id in tcg_forbidden_ids:
                    tcg_limit = "forbidden"
                elif card_id in tcg_limited_ids:
                    tcg_limit = "limited"
                elif card_id in tcg_semi_limited_ids:
                    tcg_limit = "semi-limited"
                md_limit = None
                if card_id in md_forbidden_ids:
                    md_limit = "forbidden"
                elif card_id in md_limited_ids:
                    md_limit = "limited"
                elif card_id in md_semi_limited_ids:
                    md_limit = "semi-limited"
                limit_array = [
                    {"ocg": ocg_limit},
                    {"tcg": tcg_limit},
                    {"md": md_limit}
                ]
                card_info['limit'] = limit_array
            print(f"已为所有卡片添加limit属性,格式为[{{'ocg': 状态}}, {{'tcg': 状态}}, {{'md': 状态}}]")
        except Exception as e:
            print(f"处理限制列表时出错: {e},卡片将没有limit属性")

        with open(output_file, 'w', encoding='utf-8') as f:
            json.dump(result, f, ensure_ascii=False, indent=2)
        print(f"成功将结果保存到 {output_file}")
    except Exception as e:
        print(f"保存结果到 {output_file} 时出错: {e}")
        sys.exit(1)
if __name__ == "__main__":
    main()
