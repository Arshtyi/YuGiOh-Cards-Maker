#!/bin/zsh
SCRIPT_DIR=$(dirname "$0")
cd "$SCRIPT_DIR/.." || exit 1
TMP_DIR="tmp"
mkdir -p "$TMP_DIR"
wget -q https://ygocdb.com/api/v0/cards.zip -O "$TMP_DIR/ygocdb_cards.zip"
unzip -q -o "$TMP_DIR/ygocdb_cards.zip" -d "$TMP_DIR"
mv "$TMP_DIR/cards.json" "$TMP_DIR/ygocdb_cards.json"
rm "$TMP_DIR/ygocdb_cards.zip"
curl -s https://db.ygoprodeck.com/api/v7/cardinfo.php | jq . > "$TMP_DIR/ygoprodeck_cardinfo.json"
mkdir -p "$TMP_DIR/figure"
THREAD_NUM=100
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
    wget -q --timeout=30 --tries=3 "$url" -O "tmp/figure/$filename" || echo "下载失败: $url"
fi
EOF
chmod +x "$TMP_DIR/download_worker.sh"
timeout 3600 bash -c "cat \"$TMP_DIR/image_urls.txt\" | xargs -P $THREAD_NUM -I {} ./\"$TMP_DIR/download_worker.sh\" {}"
if [ $? -eq 124 ]; then
    echo "警告: 下载操作超时（1小时），部分图片可能未下载完成"
fi
DOWNLOADED=$(ls -1 "$TMP_DIR/figure" 2>/dev/null | wc -l)
echo "下载完成！成功下载 $DOWNLOADED 张图片到 $TMP_DIR/figure 目录"
if [ $DOWNLOADED -eq 0 ]; then
    echo "警告: 没有成功下载任何图片，请检查网络连接或URL列表"
elif [ $DOWNLOADED -lt $TOTAL ]; then
    echo "提示: 部分图片可能未能下载，成功率: $(($DOWNLOADED * 100 / $TOTAL))%"
fi
rm -f "$TMP_DIR/image_urls.txt"
rm -f "$TMP_DIR/download_worker.sh"
exit 0