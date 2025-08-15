import json
import os
import requests
from lxml import html
def get_card_id(card_name):
    """
    通过ygocdb.com获取卡片ID
    """
    headers = {
        'User-Agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36',
        'Accept': 'text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8',
        'Accept-Language': 'zh-CN,zh;q=0.9,en;q=0.8',
        'Referer': 'https://ygocdb.com/',
        'Connection': 'keep-alive',
        'Cache-Control': 'max-age=0',
    }
    search_url = f"https://ygocdb.com/?search={card_name}"
    try:
        response = requests.get(search_url, headers=headers)
        response.raise_for_status()
        tree = html.fromstring(response.content)
        card_id_element = tree.xpath('/html/body/main/div/div[2]/div[2]/h3[3]/span[1]')
        if card_id_element and len(card_id_element) > 0:
            card_id_str = card_id_element[0].text.strip()
            card_id = int(card_id_str)
            print(f"卡片 '{card_name}' 的ID: {card_id}")
            return card_id
        else:
            print(f"警告: 无法找到卡片 '{card_name}' 的ID")
            return None
    except Exception as e:
        print(f"获取卡片 '{card_name}' ID时出错: {str(e)}")
        return None
def update_md_banlist(url, output_path):
    """
    从指定URL获取Master Duel (MD)禁限卡表并更新文件
    MD的表格结构特殊,需要从每行的状态列(td[4])获取禁限级别
    """
    headers = {
        'User-Agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36',
        'Accept': 'text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8',
        'Accept-Language': 'zh-CN,zh;q=0.9,en;q=0.8',
        'Referer': 'https://yugipedia.com/',
        'Connection': 'keep-alive',
        'Cache-Control': 'max-age=0',
    }
    print(f"正在处理Master Duel禁限卡表: {url}")
    try:
        response = requests.get(url, headers=headers)
        response.raise_for_status()
        tree = html.fromstring(response.content)
        table_xpath = "/html/body/div[3]/div[3]/div[4]/div/table[3]/tbody/tr"
        rows = tree.xpath(table_xpath)
        result = {
            "forbidden": [],
            "limited": [],
            "semi-limited": []
        }
        for row in rows[1:] if rows else []:
            card_name_elements = row.xpath("td[1]/a")
            if not card_name_elements:
                continue
            status_elements = row.xpath("td[4]")
            if not status_elements:
                continue
            card_name = card_name_elements[0].text.strip()
            if not card_name:
                card_name = card_name_elements[0].get("title", "").strip()
            if not card_name:
                continue
            status = status_elements[0].text.strip().lower()
            if status == "unlimited" or status == "":
                continue
            if status in ["forbidden", "limited", "semi-limited"]:
                card_id = get_card_id(card_name)
                if card_id:
                    result[status].append(card_id)
            else:
                print(f"警告: 未知的禁限状态 '{status}' 用于卡片 '{card_name}'")

        # 输出各禁限级别的卡片数量
        forbidden_count = len(result["forbidden"])
        limited_count = len(result["limited"])
        semi_limited_count = len(result["semi-limited"])
        total_count = forbidden_count + limited_count + semi_limited_count

        print(f"Master Duel禁限卡表统计:")
        print(f"  禁止卡片数量: {forbidden_count}")
        print(f"  限制卡片数量: {limited_count}")
        print(f"  准限制卡片数量: {semi_limited_count}")
        print(f"  总禁限卡片数量: {total_count}")

        os.makedirs(os.path.dirname(output_path), exist_ok=True)
        with open(output_path, 'w', encoding='utf-8') as f:
            json.dump(result, f, indent=4)
        print(f"Master Duel禁限卡表已成功更新至 {output_path}")
    except Exception as e:
        print(f"更新Master Duel禁限卡表时出错: {str(e)}")
def update_banlist_with_custom_url(url, output_path):
    """
    从指定URL获取禁限卡表并更新文件
    """
    headers = {
        'User-Agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36',
        'Accept': 'text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8',
        'Accept-Language': 'zh-CN,zh;q=0.9,en;q=0.8',
        'Referer': 'https://yugipedia.com/',
        'Connection': 'keep-alive',
        'Cache-Control': 'max-age=0',
    }
    print(f"正在处理禁限卡表链接: {url}")
    try:
        response = requests.get(url, headers=headers)
        response.raise_for_status()
        tree = html.fromstring(response.content)
        xpath_paths = {
            "forbidden": "/html/body/div[3]/div[3]/div[4]/div/table[1]/tbody/tr",
            "limited": "/html/body/div[3]/div[3]/div[4]/div/table[2]/tbody/tr",
            "semi-limited": "/html/body/div[3]/div[3]/div[4]/div/table[3]/tbody/tr"
        }
        result = {
            "forbidden": [],
            "limited": [],
            "semi-limited": []
        }
        for limit_type, xpath in xpath_paths.items():
            print(f"正在处理{limit_type}卡表...")
            rows = tree.xpath(xpath)
            for row in rows[1:] if rows else []:
                card_name_elements = row.xpath("td[1]/a")
                if not card_name_elements:
                    continue
                card_name = card_name_elements[0].text.strip()
                if not card_name:
                    card_name = card_name_elements[0].get("title", "").strip()
                if not card_name:
                    continue
                card_id = get_card_id(card_name)
                if card_id:
                    result[limit_type].append(card_id)

        # 输出各禁限级别的卡片数量
        forbidden_count = len(result["forbidden"])
        limited_count = len(result["limited"])
        semi_limited_count = len(result["semi-limited"])
        total_count = forbidden_count + limited_count + semi_limited_count

        # 从URL中提取格式名称
        format_name = "OCG" if "ocg" in url.lower() else "TCG" if "tcg" in url.lower() else "未知格式"

        print(f"{format_name}禁限卡表统计:")
        print(f"  禁止卡片数量: {forbidden_count}")
        print(f"  限制卡片数量: {limited_count}")
        print(f"  准限制卡片数量: {semi_limited_count}")
        print(f"  总禁限卡片数量: {total_count}")

        os.makedirs(os.path.dirname(output_path), exist_ok=True)
        with open(output_path, 'w', encoding='utf-8') as f:
            json.dump(result, f, indent=4)
        print(f"禁限卡表已成功更新至 {output_path}")
    except Exception as e:
        print(f"更新禁限卡表时出错: {str(e)}")

if __name__ == "__main__":
    project_root = os.path.dirname(os.path.abspath(__file__))
    headers = {
        'User-Agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36',
        'Accept': 'text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8',
        'Accept-Language': 'zh-CN,zh;q=0.9,en;q=0.8',
        'Referer': 'https://yugipedia.com/',
        'Connection': 'keep-alive',
        'Cache-Control': 'max-age=0',
    }
    yugipedia_url = "https://yugipedia.com/wiki/Yugipedia"
    print("正在从Yugipedia首页获取最新的禁限卡表链接...")
    try:
        response = requests.get(yugipedia_url, headers=headers)
        homepage_tree = html.fromstring(response.content)
        tcg_link_element = homepage_tree.xpath('/html/body/div[3]/div[3]/div[4]/div/div[1]/div[3]/div[1]/ul/li[1]/a')
        md_link_element = homepage_tree.xpath('/html/body/div[3]/div[3]/div[4]/div/div[1]/div[3]/div[1]/ul/li[6]/a')
        ocg_link_element = homepage_tree.xpath('/html/body/div[3]/div[3]/div[4]/div/div[1]/div[3]/div[1]/ul/li[3]/a')
        ocg_relative_url = ocg_link_element[0].get('href')
        ocg_url = f"https://yugipedia.com{ocg_relative_url}" if ocg_relative_url.startswith('/') else ocg_relative_url
        ocg_output_path = os.path.join(project_root, "res", "limit", "ocg.json")
        print(f"开始更新OCG禁限卡表...")
        update_banlist_with_custom_url(ocg_url, ocg_output_path)
        tcg_relative_url = tcg_link_element[0].get('href')
        tcg_url = f"https://yugipedia.com{tcg_relative_url}" if tcg_relative_url.startswith('/') else tcg_relative_url
        tcg_output_path = os.path.join(project_root, "res", "limit", "tcg.json")
        print(f"开始更新TCG禁限卡表...")
        update_banlist_with_custom_url(tcg_url, tcg_output_path)
        md_relative_url = md_link_element[0].get('href')
        md_url = f"https://yugipedia.com{md_relative_url}" if md_relative_url.startswith('/') else md_relative_url
        md_output_path = os.path.join(project_root, "res", "limit", "md.json")
        print(f"开始更新Master Duel (MD)禁限卡表...")
        update_md_banlist(md_url, md_output_path)
    except Exception as e:
        print(f"获取禁限卡表链接时出错: {str(e)}")
