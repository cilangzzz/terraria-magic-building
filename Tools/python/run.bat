@echo off
chcp 65001 >nul
echo ====================================
echo Terraria 数据工具
echo ====================================
echo.

cd /d "%~dp0"

echo 检查Python...
python --version >nul 2>&1
if errorlevel 1 (
    echo 错误: 未找到Python，请先安装Python 3.x
    pause
    exit /b 1
)

echo 检查依赖...
pip show requests >nul 2>&1
if errorlevel 1 (
    echo 正在安装依赖...
    pip install -r requirements.txt
)

echo.
echo 选择操作:
echo 1. 爬取Wiki数据 (API方式)
echo 2. 导入现有JSON数据到SQLite数据库
echo 3. 全流程 (爬取 + 导入)
echo 4. 查看数据库统计
echo.
set /p choice="输入选项 (1-4): "

if "%choice%"=="1" python tile_crawler.py
if "%choice%"=="2" python import_json_to_db.py
if "%choice%"=="3" (
    python tile_crawler.py
    python import_json_to_db.py
)
if "%choice%"=="4" python show_db_stats.py

echo.
pause