#!/bin/bash
cd "$(dirname "$0")/.." || exit 1
mkdir -p dev
OUTPUT_FILE="dev/unofficial_cards.txt"
> "$OUTPUT_FILE"
echo "开始分析非正式发售卡片..."
echo "比较 tmp/figure 目录中的文件与 tmp/cards.json 中的键..."
if [ -d "tmp/figure" ]; then
    FIGURE_IDS=$(find tmp/figure -name "*.png" -type f | sed 's|.*/\([0-9]*\)\.png|\1|g' | sort -n)
    FIGURE_COUNT=$(echo "$FIGURE_IDS" | wc -l)
    echo "在 tmp/figure 目录中找到 $FIGURE_COUNT 个PNG文件"
else
    echo "错误: tmp/figure 目录不存在"
    exit 1
fi
if [ -f "tmp/cards.json" ]; then
    CARDS_IDS=$(grep -o '"[0-9]*":' tmp/cards.json | sed 's/"//g' | sed 's/://g' | sort -n)
    CARDS_COUNT=$(echo "$CARDS_IDS" | wc -l)
    echo "在 tmp/cards.json 中找到 $CARDS_COUNT 个ID"
else
    echo "错误: tmp/cards.json 文件不存在"
    exit 1
fi
echo "正在查找存在于figure目录但不在cards.json中的ID..."
echo "$FIGURE_IDS" > /tmp/figure_ids.txt
echo "$CARDS_IDS" > /tmp/cards_ids.txt
DIFF_IDS=$(comm -23 /tmp/figure_ids.txt /tmp/cards_ids.txt)
DIFF_COUNT=$(echo "$DIFF_IDS" | grep -v "^$" | wc -l)
if [ "$DIFF_COUNT" -gt 0 ]; then
    echo "$DIFF_IDS" >> "$OUTPUT_FILE"
    echo "找到 $DIFF_COUNT 个非正式发售的ID,结果已写入 $OUTPUT_FILE"
else
    echo "没有找到非正式发售的卡片" >> "$OUTPUT_FILE"
    echo "没有找到非正式发售的卡片"
fi
rm -f /tmp/figure_ids.txt /tmp/cards_ids.txt
echo "分析完成！"
