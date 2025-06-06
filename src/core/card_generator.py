#!/usr/bin/env python3
# -*- coding: utf-8 -*-

"""
卡片图像生成器模块。
根据卡片数据生成游戏王卡片框架图像（不包含卡片名称文本）。
"""

import json
import concurrent.futures
import threading
from pathlib import Path
from PIL import Image

def select_card_frame(frame_type, asset_dir):
    """
    根据frameType选择对应的卡片框架图片
    
    Args:
        frame_type (str): 卡片的框架类型
        asset_dir (Path): 资源目录路径
    
    Returns:
        Path: 卡片框架图片的路径
    """
    # frameType到图片文件名的映射
    frame_type_map = {
        # 怪兽卡
        "normal": "card-normal.png",
        "effect": "card-effect.png",
        "ritual": "card-ritual.png",
        "fusion": "card-fusion.png",
        "synchro": "card-synchro.png",
        "xyz": "card-xyz.png",
        "link": "card-link.png",
        "token": "card-token.png",
        # 灵摆怪兽卡
        "normal_pendulum": "card-normal-pendulum.png",
        "effect_pendulum": "card-effect-pendulum.png",
        "ritual_pendulum": "card-ritual-pendulum.png",
        "fusion_pendulum": "card-fusion-pendulum.png",
        "synchro_pendulum": "card-synchro-pendulum.png",
        "xyz_pendulum": "card-xyz-pendulum.png",
        # 魔法陷阱卡
        "spell": "card-spell.png",
        "trap": "card-trap.png"
    }
    
    # 将frameType转换为小写并处理特殊情况
    if not frame_type:
        return asset_dir / "figure" / "card-normal.png"  # 默认使用普通怪兽卡
    
    frame_type = frame_type.lower()
    
    # 处理灵摆卡特殊情况
    if "pendulum" in frame_type:
        base_type = frame_type.replace("_pendulum", "").replace("pendulum_", "")
        frame_key = f"{base_type}_pendulum"
    else:
        frame_key = frame_type
    
    # 获取对应的图片文件名
    frame_file = frame_type_map.get(frame_key)
    if not frame_file:
        # 如果没有找到对应的框架，使用默认的普通怪兽卡
        frame_file = "card-normal.png"
    
    return asset_dir / "figure" / frame_file

def generate_card_image(card_data, asset_dir, output_dir, logger, root_dir=None):
    """
    生成卡片框架图像（不添加卡片名称文本）
    
    Args:
        card_data (dict): 卡片数据
        asset_dir (Path): 资源目录路径
        output_dir (Path): 输出目录路径
        logger (logging.Logger): 日志记录器
        root_dir (Path, optional): 项目根目录路径，用于保存到figure目录
    
    Returns:
        Path: 生成的卡片图像的路径
    """
    card_id = card_data.get("id")
    frame_type = card_data.get("frameType")
    card_name = card_data.get("name", "")
    
    # 获取卡片框架图片路径
    frame_path = select_card_frame(frame_type, asset_dir)
    
    logger.info(f"卡片 {card_id} 使用框架: {frame_path.name}")
    
    # 加载卡片框架图片
    try:
        frame_image = Image.open(frame_path)
        
        # 仅生成卡片框架，不添加卡片名称
        if frame_type:
            logger.info(f"仅生成卡片框架，不添加卡片名称: {card_name}")
        
        # 将输出保存到根目录下的figure目录
        if root_dir:
            figure_dir = root_dir / "figure"
            figure_dir.mkdir(parents=True, exist_ok=True)
            output_path = figure_dir / f"{card_id}.png"
        else:
            # 创建输出目录（如果不存在）
            output_dir.mkdir(parents=True, exist_ok=True)
            output_path = output_dir / f"{card_id}.png"
        
        # 保存修改后的图片
        frame_image.save(output_path)
        
        logger.info(f"成功生成卡片图像: {output_path}")
        return output_path
    except Exception as e:
        logger.error(f"生成卡片图像时出错: {e}")
        return None

def generate_cards(tmp_dir, asset_dir, output_dir, logger, root_dir=None):
    """
    从cards.json读取卡片数据并生成卡片图像
    
    Args:
        tmp_dir (Path): 临时目录路径
        asset_dir (Path): 资源目录路径
        output_dir (Path): 输出目录路径
        logger (logging.Logger): 日志记录器
        root_dir (Path, optional): 项目根目录路径，用于保存到figure目录
    """
    # 加载cards.json
    cards_file = tmp_dir / "cards.json"
    try:
        with open(cards_file, 'r', encoding='utf-8') as f:
            cards_data = json.load(f)
        
        # 限制只生成500张卡片（用于调试）
        max_cards = 500
        if len(cards_data) > max_cards:
            logger.info(f"调试模式：限制只生成{max_cards}张卡片")
            # 转换为列表，截取前500个元素，再转回字典
            cards_list = list(cards_data.items())[:max_cards]
            cards_data = dict(cards_list)
            
        logger.info(f"成功加载cards.json，将处理{len(cards_data)}张卡")
    except Exception as e:
        logger.error(f"加载cards.json时出错: {e}")
        return
    
    # 创建输出目录
    if root_dir:
        figure_dir = root_dir / "figure"
        figure_dir.mkdir(parents=True, exist_ok=True)
    else:
        output_dir.mkdir(parents=True, exist_ok=True)
    
    # 创建线程安全的计数器
    successful_count = 0
    counter_lock = threading.Lock()
    
    # 线程处理函数
    def process_card(card_id, card_data):
        nonlocal successful_count
        try:
            result = generate_card_image(card_data, asset_dir, output_dir, logger, root_dir)
            if result:
                with counter_lock:
                    successful_count += 1
            return result
        except Exception as e:
            logger.error(f"处理卡片 {card_id} 时发生错误: {e}")
            return None
    
    # 创建线程池并提交任务
    max_workers = min(1000, len(cards_data))  # 限制最大线程数
    logger.info(f"启用多线程处理，线程数: {max_workers}")
    
    with concurrent.futures.ThreadPoolExecutor(max_workers=max_workers) as executor:
        # 提交所有任务
        future_to_card = {
            executor.submit(process_card, card_id, card_data): card_id
            for card_id, card_data in cards_data.items()
        }
        
        # 显示进度
        total_cards = len(cards_data)
        completed = 0
        
        for future in concurrent.futures.as_completed(future_to_card):
            card_id = future_to_card[future]
            completed += 1
            
            # 每处理10%的卡片就输出一次进度
            if completed % max(1, total_cards // 10) == 0 or completed == total_cards:
                progress = (completed / total_cards) * 100
                logger.info(f"进度: {progress:.1f}% ({completed}/{total_cards})")
    
    logger.info(f"总共生成了 {successful_count} 张卡片图像")
