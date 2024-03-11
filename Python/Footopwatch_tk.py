import datetime
from enum import Enum
from time import time
import tkinter as tk
import tkinter.font
from pynput import keyboard


# enum class
class Action(Enum):
    start = "정지"
    stop = "리셋"
    reset = "시작"


# The key combination to check
COMBINATION = {keyboard.Key.space, keyboard.Key.ctrl_l}
# The currently active modifiers
curAction = Action.reset
initTimer = "00:00.00"
running = False
current = set()
timer = 0


def actionTimer():
    global curAction
    if curAction.name == "reset":
        curAction = Action.start
        start()
    elif curAction.name == "start":
        curAction = Action.stop
        stop()
    else:
        curAction = Action.reset
        initial()
    startButton.config(text=curAction.value)


def startTimer():
    if running:
        global initTime
        deltaTime = time() - initTime
        # timeText.configure(text=f"{deltaTime//60:0>2d}:{deltaTime:0>2.2f}")
        timeText.configure(
            text="{0:0>2}:{1:0>5.2f}".format(int(deltaTime // 60), deltaTime)
        )
    app.after(10, startTimer)


def start():
    global running
    running = True
    global initTime
    initTime = time()


def stop():
    global running
    running = False


def initial():
    global running
    running = False
    global timer
    timer = 0
    timeText.configure(text=initTimer)


bgColor = "#616161"
btnColor = "#424242"
fontColor = "#FFFFFF"
info = "ctrl+space: Foot 스위치\nesc: 종료"


app = tk.Tk()
app.title("Footopwatch v1.0")
app.geometry("200x170-10+50")
app.attributes("-toolwindow", True)
app.wm_attributes("-topmost", 1)
app.config(bg=bgColor)
timerFont = tkinter.font.Font(size=30)
btnFont = tkinter.font.Font(size=12, weight="bold")
infoFont = tkinter.font.Font(size=10)

frame = tk.Frame(app, bg=bgColor, borderwidth=1)
frame.pack(fill="both", expand=True)

timeText = tk.Label(frame, text=initTimer, bg=bgColor, fg=fontColor, font=timerFont)
timeText.pack(side="top", fill="x", expand=True)

startButton = tk.Button(
    frame, text="시작", bg=btnColor, fg=fontColor, font=btnFont, command=actionTimer
)
startButton.pack(side="bottom", fill="x")

infoText = tk.Label(
    frame,
    text=info,
    anchor="e",
    justify="right",
    font=infoFont,
    bg=bgColor,
    fg=fontColor,
    padx=3,
    pady=3,
)
infoText.pack(side="bottom", fill="x")

startTimer()


def on_press(key):
    # print(key)
    if key in COMBINATION:
        current.add(key)
        if all(k in current for k in COMBINATION):
            # print("All modifiers active!")
            actionTimer()
    if key == keyboard.Key.esc:
        on_closing()


def on_release(key):
    try:
        current.remove(key)
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
