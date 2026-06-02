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
echo 1. 初始化数据库
echo 2. 爬取Wiki数据
echo 3. 生成向量 (推荐)
echo 4. 查看数据库统计
echo 5. 全流程 (初始化 + 向量)
echo.
set /p choice="输入选项 (1-5): "

if "%choice%"=="1" cd database && python init_full_db.py && cd ..
if "%choice%"=="2" cd crawler && python tile_crawler.py && cd ..
if "%choice%"=="3" cd vector && python generate_embeddings_smart.py && cd ..
if "%choice%"=="4" cd database && python show_db_stats.py && cd ..
if "%choice%"=="5" (
    cd database && python init_full_db.py && cd ..
    cd vector && python generate_embeddings_smart.py && cd ..
)

echo.
pause