from scanClass import ScanClass
import cv2

import numpy as np
import os
import re
import win32file, win32pipe

ScanObject = ScanClass()






def thresholdImage(image,threshValue):
    roiandgray = ScanObject.RoiAndGray(image)
    threshImg = ScanObject.ThreshImage(roiandgray,threshValue)


    return threshImg

def distanceImage(imageThresh, distanceValue):
    labelsImg  =ScanObject.LocalImage(imageThresh,distanceValue)


    return labelsImg



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

        stringFromServer = data.decode(encoding='utf-8', errors='ignore')
        dataString = re.search('\$START\$(.*)\$END\$', stringFromServer).group(1)

        print(dataString)

        commandArray = dataString.split("$$$")
        
        #[thresh, 40, url]
        if commandArray[0] == "thresh":

            image = cv2.imread(commandArray[2])
            image = cv2.resize(image, (828, 567))
            threshValue =  int(commandArray[1])
            thresh = thresholdImage(image, threshValue)

            cv2.imwrite('output.jpg', thresh)

            #current working directory D:\\aadsd\output.jpg
            outputImageThreshUrl = "$START$" + os.getcwd() + "\\output.jpg" + "$END$"
            
            win32file.WriteFile(fileHandle,bytes(outputImageThreshUrl,"UTF-8"),None)

        elif commandArray[0] == "distance":

            if int(commandArray[1]) == 0 :
                outputImageDistanceUrl =  "$START$" + "NO_IMAGE" + "$END$"
                win32file.WriteFile(fileHandle,bytes(outputImageDistanceUrl, "UTF-8"),None)
                continue

         

            distanceValue = int(commandArray[1])
            threshValue = int(commandArray[2])

            imageCrop = cv2.imread(commandArray[3])
            ImgCropresize = cv2.resize(imageCrop, (828, 567))

            imageThresh = thresholdImage(ImgCropresize, threshValue)           
            labelsImg = distanceImage(imageThresh, distanceValue)
            roiandgray = ScanObject.RoiAndGray(ImgCropresize)
            outputImg,bacteriaCenters,total = ScanObject.CountColoni(labelsImg,roiandgray,ImgCropresize)
            print("bacteria", bacteriaCenters)
            cv2.imwrite("outputDistance.jpg",outputImg)
            print("finish written image")

            outputImageDistanceUrl =  "$START$" + os.getcwd() + "\\outputDistance.jpg" + "$END$"
            #{  }
            win32file.WriteFile(fileHandle,bytes(outputImageDistanceUrl, "UTF-8"),None)


        elif commandArray[0] == "count":
            outputTotal = "$START$" + str(total) + "$END$"
            win32file.WriteFile(fileHandle, bytes(outputTotal,"UTF-8"), None)

            ##Python mac dinh, json python

        elif commandArray[0] == "centers":
            #$START$290,63,.....$end$
            outputCenter = "$START$"

            for bacteria in bacteriaCenters:
                outputCenter += "{},{},".format(bacteria[0], bacteria[1])

            #$START$290,63,340,64
            outputCenter = outputCenter[:-1]

            outputCenter += "$END$"
            
            print("centers", outputCenter)

            win32file.WriteFile(fileHandle, bytes(outputCenter,"UTF-8"),None)

main()