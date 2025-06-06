#!/usr/bin/env python3
# -*- coding: utf-8 -*-

"""
主程序入口。
负责调用src目录下的各个模块实现功能。
"""

from pathlib import Path
import shutil
import os

# 导入自定义模块
from src.util.logger import setup_logger
from src.core.card_processor import process_cards
from src.core.card_generator import generate_cards

# 设置项目根目录和其他目录
PROJECT_ROOT = Path(__file__).parent.absolute()
TMP_DIR = PROJECT_ROOT / "tmp"
LOG_DIR = PROJECT_ROOT / "log"
ASSET_DIR = PROJECT_ROOT / "asset"
OUTPUT_DIR = PROJECT_ROOT / "output"
FIGURE_DIR = PROJECT_ROOT / "figure"

def clear_directory(directory_path, keep_dir=True):
    """
    清空指定目录中的所有文件
    
    Args:
        directory_path (Path): 要清空的目录路径
        keep_dir (bool): 是否保留目录本身，默认为True
    """
    # 确保目录存在
    directory_path.mkdir(parents=True, exist_ok=True)
    
    # 删除目录中的所有文件
    for item in directory_path.glob('*'):
        if item.is_file():
            try:
                item.unlink()
            except (PermissionError, OSError) as e:
                print(f"无法删除文件 {item}: {e}")
        elif item.is_dir() and not keep_dir:
            try:
                shutil.rmtree(item)
            except (PermissionError, OSError) as e:
                print(f"无法删除目录 {item}: {e}")

def main():
    """主函数"""
    # 清空log和figure文件夹
    clear_directory(LOG_DIR)
    clear_directory(FIGURE_DIR)
    
    # 设置日志记录器
    logger = setup_logger(LOG_DIR)
    
    logger.info("程序开始执行")
    logger.info("已清空log和figure文件夹")
    
    # 处理卡片数据
    process_cards(TMP_DIR, logger)
    
    # 生成卡片图像
    generate_cards(TMP_DIR, ASSET_DIR, OUTPUT_DIR, logger, PROJECT_ROOT)
    
    logger.info("程序执行完毕")

if __name__ == "__main__":
    main()