import requests
import json

# ================= 配置区 =================
# 把你在 Unity 面板里填的参数复制到这里
API_KEY = "sk-2af0ba2c32aa4bf498daada743d7d112" 
API_URL = "https://dashscope.aliyuncs.com/compatible-mode/v1/chat/completions" # 务必注意这里要有 /v1/chat/completions
MODEL_NAME = "qwen3.5-122b-a10b" 
# ==========================================

def test_llm_api():
    headers = {
        "Content-Type": "application/json",
        "Authorization": f"Bearer {API_KEY}"
    }

    # 构建一个最简单的测试请求，不发太复杂的上下文
    payload = {
        "model": MODEL_NAME,
        "messages": [
            {"role": "user", "content": "Hello, this is a test. Please reply with exactly 'API_OK'."}
        ],
        "temperature": 0.1,
        "max_tokens": 10
    }

    print(f"🚀 开始测试 API...")
    print(f"🔗 目标地址: {API_URL}")
    print(f"🤖 目标模型: {MODEL_NAME}\n")

    try:
        # 发送 POST 请求，设置 15 秒超时
        response = requests.post(API_URL, headers=headers, json=payload, timeout=15)
        
        print(f"HTTP 状态码: {response.status_code}")
        
        if response.status_code == 200:
            print("\n✅ 测试成功！API Key、URL 和模型名称均有效。")
            print("💬 模型回复:", response.json()["choices"][0]["message"]["content"])
            print("👉 结论: 既然 Python 能跑通，如果 Unity 里还是 404，说明是 Unity 没吃你的系统代理，或者网络被拦截了。")
            
        elif response.status_code == 404:
            print("\n❌ 404 Not Found: 请求的地址不存在或模型无法匹配！")
            print("👉 检查建议:")
            print("   1. 检查 URL 结尾是否少写了 /v1/chat/completions")
            print("   2. 检查模型名称是否拼写错误")
            
        elif response.status_code == 401:
            print("\n❌ 401 Unauthorized: 身份验证失败！")
            print("👉 检查建议: 你的 API Key 填错了，或者余额不足/已过期。")
            
        else:
            print(f"\n⚠️ 其他错误: {response.text}")
            
    except requests.exceptions.ProxyError:
         print("\n🚨 代理错误: Python 无法通过你的本地代理连接目标服务器。")
    except requests.exceptions.Timeout:
         print("\n🚨 请求超时: 连接服务器耗时过长。请检查是否需要开启科学上网，或者你用的国内模型 API 宕机了。")
    except requests.exceptions.RequestException as e:
        print(f"\n🚨 网络请求异常: {e}")

if __name__ == "__main__":
    test_llm_api()