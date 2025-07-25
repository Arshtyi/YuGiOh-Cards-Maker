#!/bin/bash
cd "$(dirname "$0")/.." || exit 1
mkdir -p dev
OUTPUT_FILE="dev/debug_results.txt"
> "$OUTPUT_FILE"
echo "开始分析figure目录与debug.txt中ID的差异..."
if [ -d "figure" ]; then
    FIGURE_IDS=$(find figure -name "*.jpg" -type f | sed 's|.*/\([0-9]*\)\.jpg|\1|g' | sort -n)
    FIGURE_COUNT=$(echo "$FIGURE_IDS" | wc -l)
    echo "在figure目录中找到 $FIGURE_COUNT 个JPG文件"
else
    echo "错误: figure目录不存在"
    exit 1
fi
if [ -f "dev/debug.txt" ]; then
    DEBUG_IDS=$(grep -o '^[0-9]*$' dev/debug.txt | sort -n)
    DEBUG_COUNT=$(echo "$DEBUG_IDS" | wc -l)
    echo "在dev/debug.txt中找到 $DEBUG_COUNT 个ID"
else
    echo "错误: dev/debug.txt文件不存在"
    exit 1
fi
echo "$FIGURE_IDS" | grep -v "^$" > /tmp/figure_ids.txt
echo "$DEBUG_IDS" | grep -v "^$" > /tmp/debug_ids.txt
sort -n /tmp/figure_ids.txt -o /tmp/figure_ids.txt
sort -n /tmp/debug_ids.txt -o /tmp/debug_ids.txt
echo "分析结果：" > "$OUTPUT_FILE"
echo "===========================================" >> "$OUTPUT_FILE"
echo "【在debug.txt中存在但在figure目录中不存在的ID】" >> "$OUTPUT_FILE"
MISSING_IDS=$(comm -23 /tmp/debug_ids.txt /tmp/figure_ids.txt)
MISSING_COUNT=$(echo "$MISSING_IDS" | grep -v "^$" | wc -l)
if [ "$MISSING_COUNT" -gt 0 ]; then
    echo "$MISSING_IDS" >> "$OUTPUT_FILE"
    echo "总计: $MISSING_COUNT 个ID在debug.txt中存在但在figure目录中不存在" >> "$OUTPUT_FILE"
else
    echo "没有找到在debug.txt中存在但在figure目录中不存在的ID" >> "$OUTPUT_FILE"
fi
echo "" >> "$OUTPUT_FILE"
echo "===========================================" >> "$OUTPUT_FILE"
echo "【在figure目录中存在但在debug.txt中不存在的ID】" >> "$OUTPUT_FILE"
EXTRA_IDS=$(comm -13 /tmp/debug_ids.txt /tmp/figure_ids.txt)
EXTRA_COUNT=$(echo "$EXTRA_IDS" | grep -v "^$" | wc -l)
if [ "$EXTRA_COUNT" -gt 0 ]; then
    echo "$EXTRA_IDS" >> "$OUTPUT_FILE"
    echo "总计: $EXTRA_COUNT 个ID在figure目录中存在但在debug.txt中不存在" >> "$OUTPUT_FILE"
else
    echo "没有找到在figure目录中存在但在debug.txt中不存在的ID" >> "$OUTPUT_FILE"
fi
echo "" >> "$OUTPUT_FILE"
echo "===========================================" >> "$OUTPUT_FILE"
echo "【统计信息】" >> "$OUTPUT_FILE"
echo "figure目录中的文件数量: $FIGURE_COUNT" >> "$OUTPUT_FILE"
echo "debug.txt中的ID数量: $DEBUG_COUNT" >> "$OUTPUT_FILE"
COMMON_COUNT=$((FIGURE_COUNT - EXTRA_COUNT))
echo "两者共有的ID数量: $COMMON_COUNT" >> "$OUTPUT_FILE"
rm -f /tmp/figure_ids.txt /tmp/debug_ids.txt
echo "分析完成！结果已写入 $OUTPUT_FILE"
