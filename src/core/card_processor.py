#!/usr/bin/env python3
# -*- coding: utf-8 -*-

"""
卡片处理模块。
处理卡片数据，从ygoprodeck_cardinfo.json和ygocdb_cards.json中提取信息，
生成新的JSON文件，以卡片ID为键，对应一个包含卡片中文名称的数组。
"""

import json
from pathlib import Path


def load_typeline_conf(conf_path, logger):
    """
    加载typeline配置文件
    
    Args:
        conf_path (Path): 配置文件路径
        logger (logging.Logger): 日志记录器
        
    Returns:
        dict: typeline翻译字典
    """
    typeline_dict = {}
    try:
        with open(conf_path, 'r', encoding='utf-8') as f:
            for line in f:
                line = line.strip()
                # 跳过注释和空行
                if not line or line.startswith('//'):
                    continue
                if '=' in line:
                    key, value = line.split('=', 1)
                    typeline_dict[key.strip()] = value.strip()
        logger.info(f"成功加载typeline配置，包含{len(typeline_dict)}个翻译项")
    except Exception as e:
        logger.error(f"加载typeline配置文件时出错: {e}")
    return typeline_dict


def process_cards(tmp_dir, logger):
    """
    处理卡片数据，从ygoprodeck_cardinfo.json和ygocdb_cards.json中提取信息
    生成新的JSON文件，以卡片ID为键，对应一个包含卡片中文名称的数组
    
    Args:
        tmp_dir (Path): 临时目录路径
        logger (logging.Logger): 日志记录器
    """
    logger.info("开始处理卡片数据")
    
    # 加载typeline配置
    typeline_conf_path = Path(__file__).parent.parent.parent / "res" / "typeline.conf"
    typeline_dict = load_typeline_conf(typeline_conf_path, logger)
    
    # 加载ygoprodeck_cardinfo.json
    prodeck_file = tmp_dir / "ygoprodeck_cardinfo.json"
    try:
        with open(prodeck_file, 'r', encoding='utf-8') as f:
            prodeck_data = json.load(f)
        logger.info(f"成功加载ygoprodeck_cardinfo.json，包含{len(prodeck_data.get('data', []))}张卡")
    except Exception as e:
        logger.error(f"加载ygoprodeck_cardinfo.json时出错: {e}")
        return
    
    # 加载ygocdb_cards.json
    cdb_file = tmp_dir / "ygocdb_cards.json"
    try:
        with open(cdb_file, 'r', encoding='utf-8') as f:
            cdb_data = json.load(f)
        logger.info(f"成功加载ygocdb_cards.json，包含{len(cdb_data)}条记录")
    except Exception as e:
        logger.error(f"加载ygocdb_cards.json时出错: {e}")
        return
    
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
    
    logger.info(f"从ygocdb_cards.json中提取了{len(id_to_cn_name)}个ID到中文名称的映射")
    logger.info(f"从ygocdb_cards.json中提取了{len(id_to_description)}个卡片描述")
    logger.info(f"从ygocdb_cards.json中提取了{len(id_to_pendulum_description)}个灵摆卡描述")
    
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
    
    logger.info(f"从ygoprodeck_cardinfo.json中提取了{len(id_to_scale)}个灵摆刻度")
    logger.info(f"从ygoprodeck_cardinfo.json中提取了{len(id_to_attribute)}个怪兽属性")
    logger.info(f"从ygoprodeck_cardinfo.json中提取了{len(id_to_atk)}个怪兽攻击力")
    logger.info(f"从ygoprodeck_cardinfo.json中提取了{len(id_to_def)}个怪兽防御力")
    logger.info(f"从ygoprodeck_cardinfo.json中提取了{len(id_to_level)}个怪兽等级")
    
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
                logger.warning(f"卡片ID {card_id} 没有找到对应的中文名称，跳过该卡片")
    
    logger.info(f"处理完成，共生成{len(result)}张卡的数据")
    
    # 保存结果到新的JSON文件
    output_file = tmp_dir / "cards.json"
    try:
        with open(output_file, 'w', encoding='utf-8') as f:
            json.dump(result, f, ensure_ascii=False, indent=2)
        logger.info(f"成功将结果保存到 {output_file}")
    except Exception as e:
        logger.error(f"保存结果到 {output_file} 时出错: {e}")
