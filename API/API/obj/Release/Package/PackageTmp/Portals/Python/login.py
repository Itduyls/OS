import json
import ast
from selenium import webdriver
from selenium.webdriver.chrome.service import Service
import time
from selenium.webdriver.common.by import By
from helper.helper import Helper, Const
from selenium.webdriver.chrome.options import Options
from datetime import datetime


models = [
    {
        "STT": 1,
        "Step_Name": "Truy cập trang Login",
        "Mota": "Nhìn thấy Text \"Đăng nhập\" trên màn hình",
        "IsPass": False
    },
    {
        "STT": 2,
        "Step_Name": "Cảnh báo khi không nhập tên đăng nhập",
        "Mota": "Hiển thị thông báo \"Tên đăng nhập không được để trống!\" khi không nhập tên",
        "IsPass": False
    },
    {
        "STT": 3,
        "Step_Name": "Cảnh báo khi không nhập mật khẩu",
        "Mota": "Hiển thị thông báo \"Mật khẩu không được để trống!\" khi không nhập tên",
        "IsPass": False
    },
    {
        "STT": 4,
        "Step_Name": "Cảnh báo khi nhập mật khẩu ít hơn 8 ký tự",
        "Mota": "Hiển thị thông báo \"Mật khẩu không được ít hơn 8 ký tự!\" khi không nhập tên",
        "IsPass": False
    },
    {
        "STT": 5,
        "Step_Name": "Tên đăng nhập hoặc mật khẩu không đúng",
        "Mota": "Hiển thị thông báo \"Tên đăng nhập hoặc mật khẩu không đúng!\" khi không nhập tên",
        "IsPass": False
    },
    {
        "STT": 6,
        "Step_Name": "Đăng nhập thành công",
        "Mota": "Đăng nhập thành công và chuyển sang trang chủ",
        "IsPass": False
    }
]
Case = {"Duan_ID": "VUE", "Case_Name": "Test chức năng đăng nhập", "Tukhoa": "Login",
        "IsUrl": Const.BaseUrl, "FileTest": "login.py", "NgayTest": str(datetime.now()), "ShowChrome": True, "CodeTest": ""}
Case["CodeTest"] = Helper.base64Img("login.py")
options = Options()
options.headless = False
ser = Service("chromedriver.exe")
driver = webdriver.Chrome(
    service=ser, options=options)  # import trình duyệt
driver.get(Const.BaseUrl)
time.sleep(1)
models[0]["Batdau"] = str(datetime.now())
if driver.find_element(By.CSS_SELECTOR, "#app > div > div > div.box-login > div > form > div.mb-3 > h1").text == "Đăng nhập":
    models[0]["IsPass"] = True
else:
    if driver.save_screenshot("output/login_1.png"):
        models[0]["Hinhanh"] = Helper.base64Img('output/login_1.png')
models[0]["Ketthuc"] = str(datetime.now())

Users_ID = driver.find_element(By.ID, "Users_ID")
IsPassWord = driver.find_element(By.ID, "IsPassWord")
ppassword = driver.find_element(By.CSS_SELECTOR, ".p-password")
button = driver.find_element(By.CSS_SELECTOR,
                             '#app > div > div > div.box-login > div > form > button')

button.click()
time.sleep(1)
models[1]["Batdau"] = str(datetime.now())
if 'invalid' in Users_ID.get_attribute('class').split():
    models[1]["IsPass"] = True
else:
    if driver.save_screenshot("output/login_2.png"):
        models[1]["Hinhanh"] = Helper.base64Img('output/login_2.png')
models[1]["Ketthuc"] = str(datetime.now())
models[2]["Batdau"] = str(datetime.now())
if 'invalid' in ppassword.get_attribute('class').split():
    models[2]["IsPass"] = True
else:
    if driver.save_screenshot("output/login_3.png"):
        models[2]["Hinhanh"] = Helper.base64Img('output/login_2.png')
models[2]["Ketthuc"] = str(datetime.now())
models[3]["Batdau"] = str(datetime.now())
IsPassWord.send_keys(Const.IsPassWord)
if 'invalid' in ppassword.get_attribute('class').split():
    models[3]["IsPass"] = True
else:
    if driver.save_screenshot("output/login_4.png"):
        models[3]["Hinhanh"] = Helper.base64Img('output/login_4.png')
models[3]["Ketthuc"] = str(datetime.now())
Users_ID.send_keys("09877292881")
button.click()
time.sleep(1)
models[4]["Batdau"] = str(datetime.now())
swal2 = driver.find_element(By.CSS_SELECTOR, ".swal2-html-container")
confirmswall = driver.find_element(By.CSS_SELECTOR,
                                   "body > div.swal2-container.swal2-center.swal2-backdrop-show > div > div.swal2-actions > button.swal2-confirm.swal2-styled.swal2-default-outline")
if swal2 and swal2.text == "Tên đăng nhập hoặc mật khẩu không đúng!":
    models[4]["IsPass"] = True
    time.sleep(1)
else:
    if driver.save_screenshot("output/login_5.png"):
        models[4]["Hinhanh"] = Helper.base64Img('output/login_5.png')
if confirmswall:
    confirmswall.click()
models[4]["Ketthuc"] = str(datetime.now())
models[5]["Batdau"] = str(datetime.now())
Users_ID.clear()
Users_ID.send_keys(Const.Users_ID)
time.sleep(1)
button.click()
time.sleep(2)
h2 = driver.find_element(By.CSS_SELECTOR,
                         "#app > div.flex.flex-column.flex-grow-1.h-full > div.flex.flex-row.shadow-3 > div.flex.flex-row.flex-grow-1.headerbar.align-items-center > h2")
if h2 and h2.text.strip() == "CÔNG TY CỔ PHẦN PHẦN MỀM PHƯƠNG ĐÔNG":
    models[5]["IsPass"] = True
else:
    if driver.save_screenshot("output/login_6.png"):
        models[5]["Hinhanh"] = Helper.base64Img('output/login_6.png')
driver.quit()  # thoát trình duyệt và kết thúc chương trình
models[5]["Ketthuc"] = str(datetime.now())

# Add Test Case lên Server
res = Helper.post(url="api/TestCase/Add_TestAuto64",
                  data={"Case": Case, "models": models})
