from enum import Enum
from time import time
import tkinter as tk
import tkinter.font
from pynput import keyboard


# enum class
class Action(Enum):
    start = "정지"
    stop = "시작"


# ==================================================================== Func
curAction = Action.stop
running = False
timer = 0
timeIndex = 1


def actionTimer():
    global curAction
    if curAction.name == "stop":
        curAction = Action.start
        start()
    elif curAction.name == "start":
        curAction = Action.stop
        stop()
    startButton.config(text=curAction.value)


def startTimer():
    if running:
        global initTime, deltaTime
        deltaTime = time() - initTime
        # timeText.configure(text=f"{deltaTime//60:0>2d}:{deltaTime:0>2.2f}")
        timeText.configure(text=returnTimeText(deltaTime))
    app.after(10, startTimer)


def start():
    global running
    running = True
    global initTime
    initTime = time()


def stop():
    global running
    running = False

    updateLaptime()
    initial()


def initial():
    global timer
    timer = 0
    timeText.configure(text=INIT_TIMER)
    global timeIndex
    timeIndex += 1


def returnTimeText(time):
    return "{0:0>2}:{1:0>5.2f}".format(int(time // 60), time)


def updateLaptime():
    listbox_lapTime.insert(tk.END, f"{timeIndex:0>2} - {returnTimeText(deltaTime)}")
    listbox_lapTime.see(tk.END)


# 메모장 실행 → 랩타임 보기
def openWordpad():
    pass


# ==================================================================== GUI
APP_TITLE = "Footopwatch v1.1"
BGCOLOR = "#616161"
BTNCOLOR = "#424242"
FONTCOLOR = "#FFFFFF"
INFO_TEXT = "ctrl+space: Foot 스위치\nesc: 종료"
INIT_TIMER = "00:00.00"
LISTBOX_LINE = 10


app = tk.Tk()
app.title(APP_TITLE)
app.geometry("-10+50")
app.attributes("-toolwindow", True)
app.wm_attributes("-topmost", 1)
app.config(bg=BGCOLOR)
timerFont = tkinter.font.Font(size=30)
btnFont = tkinter.font.Font(size=12, weight="bold")
infoFont = tkinter.font.Font(size=10)

frame = tk.Frame(app, bg=BGCOLOR, padx=20, pady=10)
frame.pack(fill="both", expand=True)

timeText = tk.Label(
    frame, text=INIT_TIMER, bg=BGCOLOR, fg=FONTCOLOR, font=timerFont, padx=10, pady=10
)
timeText.pack(side="top", fill="x", expand=True)

startButton = tk.Button(
    frame, text="시작", bg=BTNCOLOR, fg=FONTCOLOR, font=btnFont, command=actionTimer
)
startButton.pack(side="bottom", fill="x")

infoText = tk.Label(
    frame,
    text=INFO_TEXT,
    anchor="e",
    justify="right",
    font=infoFont,
    bg=BGCOLOR,
    fg=FONTCOLOR,
    padx=10,
    pady=10,
)
infoText.pack(side="bottom", fill="x")

# Recording Times
frame_lapTime = tk.LabelFrame(app, text="lap times", bg=BGCOLOR, fg=FONTCOLOR)
frame_lapTime.pack(fill="x", padx=5, pady=5, ipadx=5, ipady=5)

scroll_lapTime = tk.Scrollbar(frame_lapTime, bg=BGCOLOR, width=2)
scroll_lapTime.pack(side="right", fill="y")

listbox_lapTime = tk.Listbox(
    frame_lapTime,
    yscrollcommand=scroll_lapTime.set,
    height=LISTBOX_LINE,
    bg=BGCOLOR,
    fg=FONTCOLOR,
    highlightthickness=0,
    disabledforeground=FONTCOLOR,
)
listbox_lapTime.pack(fill="x", side="bottom", padx=10, pady=5, expand=True)

scroll_lapTime.config(command=listbox_lapTime.yview)

startTimer()


# ==================================================================== Detect Key
COMBINATION = {keyboard.Key.space, keyboard.Key.ctrl_l}
currentKey = set()


def on_press(key):
    # print(key)
    if key in COMBINATION:
        currentKey.add(key)
        if all(k in currentKey for k in COMBINATION):
            # print("All modifiers active!")
            actionTimer()
    if key == keyboard.Key.esc:
        on_closing()


def on_release(key):
    try:
        currentKey.remove(key)
    except KeyError:
        pass


# 종료 프로토콜
def on_closing():
    listener.stop()
    app.destroy()


app.protocol("WM_DELETE_WINDOW", on_closing)

with keyboard.Listener(on_press=on_press, on_release=on_release) as listener:
    app.mainloop()
    listener.join()
