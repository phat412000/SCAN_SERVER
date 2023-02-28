from scanClass import ScanClass
import cv2

import numpy as np
import os

import websockets
import asyncio

import win32file, win32pipe

ScanObject = ScanClass()


def thresholdImage(image,threshValue):
    roiandgray = ScanObject.RoiAndGray(image)
    threshImg = ScanObject.ThreshImage(roiandgray,threshValue)


    return threshImg

def imageToBytes(image):
    _, buffer = cv2.imencode(".jpg", image)    

    return buffer.tobytes()

def main():
    fileHandle = win32file.CreateFile(
        "\\\\.\\pipe\\process_pipe", 
        win32file.GENERIC_READ | win32file.GENERIC_WRITE, 
        0, 
        None, 
        win32file.OPEN_EXISTING, 
        0, 
        None)
    res = win32pipe.SetNamedPipeHandleState(fileHandle, win32pipe.PIPE_READMODE_MESSAGE, None, None)

    while True:
        left, data = win32file.ReadFile(fileHandle, 4096)
        stringFromServer = data.decode("utf-8") 

        

        splitData = stringFromServer.split("$$$")

        print(splitData)

        image = cv2.imread(splitData[2])
        thresh = thresholdImage(image, int(splitData[1]))
        cv2.imwrite('output.jpg', thresh)

        print(stringFromServer)

        outputImage = os.getcwd() + "\\output.jpg"

        win32file.WriteFile(fileHandle,bytes(outputImage + "END","UTF-8"),None)

        


main()