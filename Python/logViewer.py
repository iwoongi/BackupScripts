import os


def writeLog(msg, path):
    global file
    global txtPath
    txtPath = f"{path}/log.txt"
    file = open(txtPath, "w")
    file.write(msg)
    file.close()


def openLog():
    os.startfile(txtPath)
