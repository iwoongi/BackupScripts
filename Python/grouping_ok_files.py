import os
import shutil
import threading
import time
from tkinter import filedialog
import tkinter.messagebox as msgbox
from tkinter import *
import pandas as pd


# ========================================================== Var
FILEFORMAT = ["mp4", "mov"]
CHOOSE_CSVFILE = "CSV 파일을 선택하세요."
CHOOSE_VIDEOFOLDER = "OBS 비디오파일이 있는 폴더를 선택하세요."
FILE_EXIST = "O"

sizeProcess = 0
finProcess = []
wrongDataList = []


# ========================================================== func
# 경로 유효성 체크 : curDir
def check_dirText(dirText):
    global srcPath
    srcPath = ""
    try:
        if os.path.exists(dirText):
            srcPath = dirText
        return True
    except:
        srcPath = ""
        return False


# CSV 파일 불러오기 = btn_csv_path
def road_csv():
    if check_finProcess():
        return

    file = filedialog.askopenfilename(
        title=CHOOSE_CSVFILE,
        filetypes=(
            ("CSV 파일", "*.csv"),
            ("XLSX 파일", "*.xlsx"),
            ("모든 파일", "*.*"),
        ),
    )
    if file:
        global obs_data

        if ".csv" in file:
            obs_data = pd.read_csv(file, encoding="cp949")
        elif ".xlsx" in file:
            obs_data = pd.read_excel(file, encoding="utf-8")
        else:
            msgbox.showinfo("알림", "CSV, XLSX파일을 불러오세요.")
            return

        lbl_csv_path.config(text=file)
        # print(obs_data.head())


# 작업 폴더 설정 = btn_folder_path
def set_folder():
    if check_finProcess():
        return

    global srcPath
    checkPath = check_dirText(txt_folder_path.get())

    folder_selected = filedialog.askdirectory(
        title=CHOOSE_VIDEOFOLDER, initialdir=srcPath if checkPath else None
    )

    if folder_selected:
        txt_folder_path.delete(0, END)
        txt_folder_path.insert(END, folder_selected)
        srcPath = folder_selected
    else:
        txt_folder_path.delete(0, END)
        txt_folder_path.insert(END, srcPath)


# 시작 버튼 동작 = btn_start
def find_okFile():
    if check_finProcess():
        return
    reset_processData()  # issue항목 텍스트 및 데이터 리셋

    okFileList = find_okList_inCSV()  # CSV파일에서 OK파일 목록 가져오기
    check_OkeyFolder()  # 작업폴더의 하위경로에 Okay폴더 유무 확인 and 폴더생성
    move_copy_files(okFileList)


# OK된 Take파일명을 List로 반환
def find_okList_inCSV():
    select_list = obs_data["Select"]
    take_list = obs_data["Take"]
    okList = list()

    idx = 0
    for obj in select_list:
        check = str(obj).lower()

        if check == "ok":
            okList.append(take_list.loc[idx])
        elif not (check == "ng" or check == "keep"):
            wrongDataList.append(take_list.loc[idx])

        update_progress("CSV data 탐색중...")
        idx += 1
    update_progress("CSV data 탐색완료!")

    return okList


# 하위폴더(/Okay) 유무 체크 and 생성
def check_OkeyFolder():
    global destPath
    destPath = str(f"{srcPath}/Okay")
    # print(checkFolder)

    if not os.path.exists(destPath):
        os.mkdir(destPath)


# OK된 Video파일 복사 or 이동
def move_copy_files(fileList):
    # 작업폴더 내에 mp4/mov파일 유무 확인
    checkFiles_inFolder = os.listdir(srcPath)
    isVideo = False
    for file in checkFiles_inFolder:
        isVideo = ".mp4" in file or ".mov" in file

    if not isVideo:
        msgbox.showinfo(
            "알림",
            "현재경로에 mp4/mov파일이 없습니다.\n작업경로의 폴더를 확인해주세요.",
        )
        return

    isCopy = option_var.get() == 1  # 0이동 1복사
    msg = "복사" if isCopy else "이동"
    update_progress(f"파일 {msg} 시작!")

    switch_buttons(False)
    global sizeProcess
    sizeProcess = len(fileList)

    for file in fileList:
        # do_process(file, isCopy)
        formatIndex = 0
        if os.path.exists(make_filePath(file, 0)):
            formatIndex = 0
        elif os.path.exists(make_filePath(file, 1)):
            formatIndex = 1
        else:
            formatIndex = -1

        if formatIndex == -1:
            print("file이 없습니다.")
            finProcess.append(str(file))
        else:
            fileName = str(f"{file}.{FILEFORMAT[formatIndex]}")
            src = os.path.join(srcPath, fileName)
            dest = os.path.join(destPath, fileName)

            srcSize = os.path.getsize(src)

            t1 = threading.Thread(
                name="run", target=moving_file, args=(src, dest, isCopy)
            )
            t1.daemon = True
            t1.start()

            t2 = threading.Thread(
                name="checking", target=file_checker, args=(srcSize, dest)
            )
            t2.daemon = True
            t2.start()

    checkCnt = 0
    for data in finProcess:
        if data == FILE_EXIST:
            checkCnt += 1
    if checkCnt == len(finProcess):
        switch_buttons(True)
        issue_text.config(text=f"{checkCnt}개의 파일이 처리되었습니다.")


# File Path Make
def make_filePath(name, index):
    fileName = f"{name}.{FILEFORMAT[index]}"
    return os.path.join(srcPath, fileName)


# Check File Size = Thread2
def file_checker(src_size, dest_path):
    # during = 0
    # Making sure the destination path exists
    while not os.path.exists(dest_path):
        # print("not exists")
        time.sleep(0.02)

    # Keep checking the file size till it's the same as source file
    while src_size != os.path.getsize(dest_path):
        # during = int((float(os.path.getsize(dest_path))/float(os.path.getsize(src_path))) * 100)
        # update_progress(f"{src_path}...{during}%")
        time.sleep(0.02)

    # print("percentage", 100)
    finProcess.append(FILE_EXIST)
    check_finProcess()


# Copy or Move = Thread1
def moving_file(src_path, dest_path, isCopy):
    if isCopy:
        # print("Copying....")
        shutil.copyfile(src_path, dest_path)
    else:
        # print("Moving....")
        shutil.move(src_path, dest_path)

    if os.path.exists(dest_path):
        # print("Done....")
        return True

    print("Failed...")
    return False


# 작업 완료 파일 갯수 확인
def check_finProcess():
    if btn_start["state"] == NORMAL:
        return False
    else:
        update_progress(f"{len(finProcess)}/{sizeProcess}... 작업 완료!")

        if len(finProcess) == sizeProcess:
            switch_buttons(True)
            final_issue_massage()
        return True


# Take파일 존재유무 확인
def check_fileExist(fileName):
    return os.path.exists(fileName)


# progressBar 셋팅
def update_progress(msg):
    progress_text.config(text=msg)
    app.update_idletasks()


# Disable Button
def switch_buttons(on):
    state = NORMAL if on else DISABLED
    btn_csv_path["state"] = state
    btn_folder_path["state"] = state
    btn_start["state"] = state
    # print("buttons ",on)


# 작업완료 팝업창
def final_issue_massage():
    issue_file = ""
    for data in finProcess:
        if data != FILE_EXIST:
            issue_file = f"{data}" if issue_file == "" else f"{issue_file}\n{data}"

    msg = ""
    if issue_file == "" and len(wrongDataList) == 0:
        msg = "issue 없음."
    else:
        wrongData = ""
        for data in wrongDataList:
            wrongData = f"{data}" if wrongData == "" else f"{wrongData}\n{data}"

        msg = (
            f"{issue_file}\n위 파일이 정상적으로 처리 되지 않았습니다."
            if wrongData == ""
            else f"{issue_file}\n위 파일이 정상적으로 처리 되지 않았습니다.\n===============\n{wrongData}\n위 데이터의 Select 옵션데이터를 확인해주세요."
        )

    # msg = (
    #     "issue 없음."
    #     if issue_file == ""
    #     else f"{issue_file}\n위 파일이 정상 처리 되지 않았습니다."
    # )
    issue_text.config(text=msg)
    # print(f"{issue_file}\n위 파일이 정상 처리 되지 않았습니다.")


# 메시지 리셋 & finProcess 리셋
def reset_processData():
    if btn_start["state"] == NORMAL and len(finProcess) != 0:
        finProcess.clear()
        issue_text.config(text="")


# 종료 팝업창 = btn_close
def click_exit():
    if btn_start["state"] == DISABLED:
        re = msgbox.askokcancel(
            "YES / NO",
            "프로그램을 종료하시면 진행중인 작업이 중단됩니다.\n종료하시겠습니까?",
        )
        if re == 1:
            app.quit()
    else:
        app.quit()


# =========================================================================== GUI
app = Tk()
app.title("Grouping OK Files v1.1")
# app.iconbitmap('video_folder.ico')


# CSV 프레임
csv_frame = LabelFrame(app, text="FeelCapture CSV파일")
csv_frame.pack(fill="x", padx=5, pady=4, ipady=4)

lbl_csv_path = Label(csv_frame, text=CHOOSE_CSVFILE, anchor="w")
lbl_csv_path.pack(side="left", fill="x", expand=True, padx=5, pady=4, ipady=3)

btn_csv_path = Button(csv_frame, text="찾기", width=10, command=road_csv)
btn_csv_path.pack(side="right", padx=5, pady=4)

# 작업 폴더 프레임
path_frame = LabelFrame(app, text="OBS Video 폴더")
path_frame.pack(fill="x", padx=5, pady=4, ipady=4)

txt_folder_path = Entry(path_frame, width=50)
txt_folder_path.insert(END, CHOOSE_VIDEOFOLDER)
txt_folder_path.pack(
    side="left", fill="x", expand=True, padx=5, pady=4, ipady=3
)  # 높이 변경

btn_folder_path = Button(path_frame, text="폴더찾기", width=10, command=set_folder)
btn_folder_path.pack(side="right", padx=5, pady=4)

# 옵션 프레임
frame_option = LabelFrame(app, text="옵션")
frame_option.pack(fill="x", padx=5, pady=4, ipady=4)

option_var = IntVar()
rad_move = Radiobutton(frame_option, text="이동", value=0, width=6, variable=option_var)
rad_save = Radiobutton(frame_option, text="복사", value=1, width=6, variable=option_var)
rad_move.select()

rad_move.pack(side="left", padx=5, pady=4)
rad_save.pack(side="left", padx=5, pady=4)

# 진행 상황
frame_progress = LabelFrame(app, text="진행상황")
frame_progress.pack(fill="x", padx=5, pady=5, ipady=5)

progress_text = Label(frame_progress, text="")
progress_text.pack(fill="x", side="top", padx=5)

# issue 현황
frame_issue = LabelFrame(app, text="issue")
frame_issue.pack(fill="x", padx=5, pady=5, ipady=5)

issue_text = Label(frame_issue, text="")
issue_text.pack(fill="x", side="top", padx=5)

scroll_issue = Scrollbar(frame_issue, width=2)
scroll_issue.pack(side="right", fill="y")

# 실행 프레임
frame_run = Frame(app)
frame_run.pack(fill="x", padx=5, pady=5)

btn_close = Button(frame_run, padx=5, pady=5, text="닫기", width=12, command=click_exit)
btn_close.pack(side="right", padx=5, pady=5)

btn_start = Button(
    frame_run, padx=5, pady=5, text="시작", width=12, command=find_okFile
)
btn_start.pack(side="right", padx=5, pady=5)

app.eval("tk::PlaceWindow . center")
app.resizable(True, False)
app.mainloop()
