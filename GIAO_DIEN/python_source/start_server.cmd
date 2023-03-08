 cd python_source

if exist work_env (
    echo "file exist"
) else (
    echo "make new virtual environment"
    python -m venv work_env
    
)

call work_env\Scripts\activate.bat
pip install -r requirements.txt
python server.py