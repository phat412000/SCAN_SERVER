from scanClass import ScanClass
import cv2
import numpy as np
import os
import re
import win32file, win32pipe
import time

import json

import segmentation_models_pytorch as smp

import warnings

warnings.filterwarnings("ignore")

import torch
import imutils
import math

ENCODER = 'resnet50'
ENCODER_WEIGHTS = 'imagenet'
CLASSES = ['background', 'road']
ACTIVATION = 'sigmoid' 


preprocessing_fn = smp.encoders.get_preprocessing_fn(ENCODER, ENCODER_WEIGHTS)

DEVICE = torch.device("cuda" if torch.cuda.is_available() else "cpu")
print(DEVICE)
if os.path.exists('best_model_lan2.pth'):
    best_model = torch.load('best_model_lan2.pth', map_location=DEVICE)
    print('Loaded UNet model from this run.')
else:
    best_model = None  
    print("cannot load model")


ScanObject = ScanClass()



def thresholdImage(image,threshValue):
    threshImg = ScanObject.ThreshImage(image,threshValue)
   

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
        


        if commandArray[0] == "segment":
      
            image = cv2.imread(commandArray[1])

            jsonstring = commandArray[2]
            polyLists = json.loads(jsonstring)

    
            contourPoints = [] #tong chua nhung cai mang con


            currentPolyName = -1

            for p in polyLists:

                print(p)
                name = p['polyName'] #1

                if currentPolyName != name:
                    contourPoints.append([]) 
                    currentPolyName = name
                 
                mouseX = int(p['mouseX'] * 6.5 / 3) # *6.5 vi ti le tu canvas len anh goc, / 2 vi giam anh goc di cho nhe segment
                mouseY = int(p['mouseY'] * 6.5 / 3)

                contourPoints[-1].append([mouseX, mouseY])
            
            print(contourPoints)

            segmentcontours = []

            for points in contourPoints:
                contour = np.array(points).reshape((-1,1,2)).astype(np.int32)

                segmentcontours.append(contour)



            #imageResize = cv2.resize(image, (828,567))
            image = cv2.resize(image, (int(image.shape[1]/3), int(image.shape[1]/3)))
    
            image_RGB = cv2.cvtColor(image, cv2.COLOR_BGR2RGB).astype("float32") 
 
            image_RGB = preprocessing_fn(image_RGB)
            image_RGB = np.transpose(image_RGB, (2, 0, 1)).astype("float32")
    
            mask_colony = np.zeros(image.shape, dtype='uint8')
 
            x_tensor = torch.from_numpy(image_RGB).to(DEVICE).unsqueeze(0)
            pred_mask = best_model(x_tensor)
            pred_mask = pred_mask.detach().squeeze().cpu().numpy()
            #print(pred_mask.shape)
            result_colony = np.where(pred_mask[2, :, :] < 0.5, 0, 255).astype('uint8')
            result_disk = np.where(pred_mask[1, :, :] < 0.5, 0, 255).astype('uint8') + result_colony
            kernel = np.ones((5, 5), np.uint8)
            result_disk = cv2.dilate(result_disk, kernel, iterations=1)
            result_colony = cv2.bitwise_and(result_colony, result_colony, mask=result_disk)
            mask_colony[:, :, 0] = result_colony
            mask_colony[:, :, 1] = result_colony
            mask_colony[:, :, 2] = result_colony


            GrayImg = ScanObject.GrayImage(mask_colony)

            threshImageAfterSegment = thresholdImage(GrayImg, 80)


            labelsImg = distanceImage(threshImageAfterSegment, 10)

            outputImageSegment, bacteriaCenters, total = ScanObject.CountColoni(labelsImg,GrayImg,image,segmentcontours)
        
            print("bacteria", bacteriaCenters)
            cv2.imwrite("ImageAfterSegment.jpg", outputImageSegment)
            print("finish written image")

            outputImageSegmentUrl = "$START$" + os.getcwd() + "\\ImageAfterSegment.jpg" + "$END$"

            win32file.WriteFile(fileHandle, bytes(outputImageSegmentUrl,"UTF-8"),None)


        elif commandArray[0] == "count":
            
            outputTotal = "$START$" + str(total) + "$END$"

            win32file.WriteFile(fileHandle,bytes(outputTotal, "UTF-8"),None)


        elif commandArray[0] == "edit":
            
            outputCenter = "$START$"

            for bacteria in bacteriaCenters:
                outputCenter += "{},{},".format(bacteria[0], bacteria[1])

            outputCenter = outputCenter[:-1]
            outputCenter += "$END$"

            print("centers" , outputCenter)
            win32file.WriteFile(fileHandle,bytes(outputCenter,"UTF-8"),None)

time.sleep(0.5)



main()


