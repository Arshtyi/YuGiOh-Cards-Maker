#!/usr/bin/env python3
# -*- coding: utf-8 -*-

"""
主程序入口。
负责调用src目录下的各个模块实现功能。
"""

from pathlib import Path

# 导入自定义模块
from src.util.logger import setup_logger
from src.core.card_processor import process_cards

# 设置项目根目录和其他目录
PROJECT_ROOT = Path(__file__).parent.absolute()
TMP_DIR = PROJECT_ROOT / "tmp"
LOG_DIR = PROJECT_ROOT / "log"

def main():
    """主函数"""
    # 设置日志记录器
    logger = setup_logger(LOG_DIR)
    
    logger.info("程序开始执行")
    
    # 处理卡片数据
    process_cards(TMP_DIR, logger)
    
    logger.info("程序执行完毕")

if __name__ == "__main__":
    main()