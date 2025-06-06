#!/usr/bin/env python3
# -*- coding: utf-8 -*-

"""
日志工具模块。
提供日志记录功能。
"""

import os
import logging
from datetime import datetime
from pathlib import Path

def setup_logger(log_dir):
    """
    设置日志记录器。
    
    Args:
        log_dir (Path): 日志目录路径
        
    Returns:
        logging.Logger: 配置好的日志记录器
    """
    # 确保日志目录存在
    os.makedirs(log_dir, exist_ok=True)
    
    # 清空日志目录中的所有文件
    for file in os.listdir(log_dir):
        file_path = os.path.join(log_dir, file)
        if os.path.isfile(file_path):
            try:
                os.unlink(file_path)
            except Exception as e:
                print(f"清除日志文件时出错: {e}")
    
    # 设置日志记录
    log_filename = f"card_process_{datetime.now().strftime('%Y%m%d_%H%M%S')}.log"
    logging.basicConfig(
        filename=os.path.join(log_dir, log_filename),
        level=logging.INFO,
        format='%(asctime)s - %(name)s - %(levelname)s - %(message)s'
    )
    
    return logging.getLogger('card_processor')
