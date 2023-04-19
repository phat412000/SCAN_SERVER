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
if os.path.exists('best_model_gray.pth'):
    best_model = torch.load('best_model_gray.pth', map_location=DEVICE)
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
        

#-----------------------------------------------------------------------------------------------------

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
                 
                mouseX = int(p['mouseX'] * 3.5 / 3.6) # *6.5 vi ti le tu canvas len anh goc, / 2 vi giam anh goc di cho nhe segment
                mouseY = int(p['mouseY'] * 3.5 / 3.6)

                contourPoints[-1].append([mouseX, mouseY])
            
            print(contourPoints)

            segmentcontours = []

            for points in contourPoints:
                contour = np.array(points).reshape((-1,1,2)).astype(np.int32)

                segmentcontours.append(contour)



            #imageResize = cv2.resize(image, (828,567))
            #image = cv2.resize(image, (int(image.shape[1]/3), int(image.shape[1]/3)))
            image_url = cv2.resize(image, (640,640))
            image_re = cv2.resize(image, (640,640))
            
            #image_RGB = cv2.cvtColor(image, cv2.COLOR_BGR2RGB).astype("float32") 
            
            image_v = image_re.copy()


            image_v = cv2.cvtColor(image_re, cv2.COLOR_BGR2HSV).astype("float32")
            v_channel = image_v[:, :, 2]
            image_re[:, :, 0] = v_channel
            image_re[:, :, 1] = v_channel
            image_re[:, :, 2] = v_channel
            #image = cv2.resize(image, down_points, interpolation= cv2.INTER_LINEAR)
            #mask = cv2.cvtColor(cv2.imread(self.mask_paths[i]), cv2.COLOR_BGR2RGB)
            #mask=cv2.resize(mask, down_points, interpolation= cv2.INTER_LINEAR)
            cv2.imwrite("img_re.jpg",image_re)
            image_gray = preprocessing_fn(image_re)

            cv2.imwrite("image_gray.jpg", image_gray)
            image_gray = np.transpose(image_gray, (2, 0, 1)).astype("float32")
            

            mask_colony = np.zeros(image_re.shape, dtype='uint8')
 
            x_tensor = torch.from_numpy(image_gray).to(DEVICE).unsqueeze(0)

            pred_mask = best_model(x_tensor)
            pred_mask = pred_mask.detach().squeeze().cpu().numpy()

            pred_mask = np.transpose(pred_mask, (1, 2, 0))


            result_colony = np.where(pred_mask[ :,:, 1] < 0.5, 0, 255).astype('uint8')

            cv2.imwrite("result.jpg", result_colony)

            #kernel = np.ones((5, 5), np.uint8)
            #result_disk = cv2.dilate(result_disk, kernel, iterations=1)
            #result_colony = cv2.bitwise_and(result_colony, result_colony, mask=result_disk)
            mask_colony[:, :, 0] = result_colony
            mask_colony[:, :, 1] = result_colony
            mask_colony[:, :, 2] = result_colony
            cv2.imwrite("mask_colony.jpg", mask_colony)
            print("mask_colony",mask_colony.shape)


            GrayImg = ScanObject.GrayImage(mask_colony)

            threshImageAfterSegment = thresholdImage(GrayImg, 80)


            labelsImg = distanceImage(threshImageAfterSegment, 10)

            outputImageSegment,bacteriaCenters, total, bacteriaContours, imageOrginal , bacteriaCentersScale, bacteriaContoursScale = ScanObject.CountColoni(labelsImg,GrayImg,image_url,segmentcontours,image)
        
            print("bacteria", bacteriaCenters)
            cv2.imwrite("ImageAfterSegment.jpg", imageOrginal)
            print("finish written image")

            outputImageSegmentUrl = "$START$" + os.getcwd() + "\\ImageAfterSegment.jpg" + "$END$"

            win32file.WriteFile(fileHandle, bytes(outputImageSegmentUrl,"UTF-8"),None)



#-----------------------------------------------------------------------------------------------------

        elif commandArray[0] == "send_auto_mode":
      
            image = cv2.imread(commandArray[1])
            cv2.imwrite("image.jpg", image)

            jsonstring = commandArray[2]
            polyLists = json.loads(jsonstring)

            contourPoints = [] #tong chua nhung cai mang con

            for p in polyLists:
 
                mouseX = int(p['Item1'] * 3.5 / 3.6 ) # *6.5 vi ti le tu canvas len anh goc, / 2 vi giam anh goc di cho nhe segment
                mouseY = int(p['Item2'] * 3.5 / 3.55 )

                contourPoints.append([mouseX, mouseY])

            segmentcontours = np.array(contourPoints).reshape((-1,1,2)).astype(np.int32)

            segmentcontours = [segmentcontours]

            print(segmentcontours)

            #imageResize = cv2.resize(image, (828,567))
            #image = cv2.resize(image, (int(image.shape[1]/3), int(image.shape[1]/3)))
            image_url = cv2.resize(image, (640,640))
            image_re = cv2.resize(image, (640,640))
            
            #image_RGB = cv2.cvtColor(image, cv2.COLOR_BGR2RGB).astype("float32") 
            
            image_v = image_re.copy()


            image_v = cv2.cvtColor(image_re, cv2.COLOR_BGR2HSV).astype("float32")
            v_channel = image_v[:, :, 2]
            image_re[:, :, 0] = v_channel
            image_re[:, :, 1] = v_channel
            image_re[:, :, 2] = v_channel
            #image = cv2.resize(image, down_points, interpolation= cv2.INTER_LINEAR)
            #mask = cv2.cvtColor(cv2.imread(self.mask_paths[i]), cv2.COLOR_BGR2RGB)
            #mask=cv2.resize(mask, down_points, interpolation= cv2.INTER_LINEAR)
            cv2.imwrite("img_re.jpg",image_re)
            image_gray = preprocessing_fn(image_re)

            cv2.imwrite("image_gray.jpg", image_gray)
            image_gray = np.transpose(image_gray, (2, 0, 1)).astype("float32")
            

            mask_colony = np.zeros(image_re.shape, dtype='uint8')
 
            x_tensor = torch.from_numpy(image_gray).to(DEVICE).unsqueeze(0)

            pred_mask = best_model(x_tensor)
            pred_mask = pred_mask.detach().squeeze().cpu().numpy()

            pred_mask = np.transpose(pred_mask, (1, 2, 0))



            result_colony = np.where(pred_mask[ :,:, 1] < 0.5, 0, 255).astype('uint8')

            cv2.imwrite("result.jpg", result_colony)

            
            #kernel = np.ones((5, 5), np.uint8)
            #result_disk = cv2.dilate(result_disk, kernel, iterations=1)
            #result_colony = cv2.bitwise_and(result_colony, result_colony, mask=result_disk)
            mask_colony[:, :, 0] = result_colony
            mask_colony[:, :, 1] = result_colony
            mask_colony[:, :, 2] = result_colony
            cv2.imwrite("mask_colony.jpg", mask_colony)

            #image_re co kich thuoc 640x640 la anh dau vao cua predict
            #lay anh 640x640 de dua vao ham gray ta co anh muc xam -> anh wastersheld
            # co duoc anh wastershelp roi thi bat dau findcontour

            GrayImg = ScanObject.GrayImage(mask_colony)

            threshImageAfterSegment = thresholdImage(GrayImg, 80)

            labelsImg = distanceImage(threshImageAfterSegment, 10)

            outputImageSegment, bacteriaCenters, total,_, imageOrginal, bacteriaCentersScale, bacteriaContoursScale  = ScanObject.CountColoni(labelsImg,GrayImg,image_url,segmentcontours,image)
            
            cv2.imwrite("ImageAfterSegment.jpg", outputImageSegment)
            cv2.imwrite("imageOriginal.jpg", imageOrginal)
            print("finish written image")

            jsonResponse = {
                "centers": bacteriaCentersScale,
                "image": os.getcwd() + "\\imageOriginal.jpg"
            }
            jsonResponse = "$START$" + json.dumps(jsonResponse) + "$END$"

            win32file.WriteFile(fileHandle, bytes(jsonResponse,"UTF-8"),None)

        


#-----------------------------------------------------------------------------------------------------

        elif commandArray[0] == "processpoint":
            print("taked")
            statefulJsonString = commandArray[1]

            statefulPointList = json.loads(statefulJsonString)

            print(statefulPointList)
          
            image = cv2.imread(commandArray[2])

            image_url = cv2.resize(image, (640,640))
            image_re = cv2.resize(image, (640,640))
            
            #image_RGB = cv2.cvtColor(image, cv2.COLOR_BGR2RGB).astype("float32") 
            
            image_v = image_re.copy()


            image_v = cv2.cvtColor(image_re, cv2.COLOR_BGR2HSV).astype("float32")
            v_channel = image_v[:, :, 2]
            image_re[:, :, 0] = v_channel
            image_re[:, :, 1] = v_channel
            image_re[:, :, 2] = v_channel
            #image = cv2.resize(image, down_points, interpolation= cv2.INTER_LINEAR)
            #mask = cv2.cvtColor(cv2.imread(self.mask_paths[i]), cv2.COLOR_BGR2RGB)
            #mask=cv2.resize(mask, down_points, interpolation= cv2.INTER_LINEAR)
            cv2.imwrite("img_re.jpg",image_re)
            image_gray = preprocessing_fn(image_re)

            cv2.imwrite("image_gray.jpg", image_gray)
            image_gray = np.transpose(image_gray, (2, 0, 1)).astype("float32")
            

            mask_colony = np.zeros(image_re.shape, dtype='uint8')
 
            x_tensor = torch.from_numpy(image_gray).to(DEVICE).unsqueeze(0)

            pred_mask = best_model(x_tensor)
            pred_mask = pred_mask.detach().squeeze().cpu().numpy()

            pred_mask = np.transpose(pred_mask, (1, 2, 0))


            result_colony = np.where(pred_mask[ :,:, 1] < 0.5, 0, 255).astype('uint8')

            cv2.imwrite("result.jpg", result_colony)

            #kernel = np.ones((5, 5), np.uint8)
            #result_disk = cv2.dilate(result_disk, kernel, iterations=1)
            #result_colony = cv2.bitwise_and(result_colony, result_colony, mask=result_disk)
            mask_colony[:, :, 0] = result_colony
            mask_colony[:, :, 1] = result_colony
            mask_colony[:, :, 2] = result_colony
            cv2.imwrite("mask_colony.jpg", mask_colony)


            GrayImg = ScanObject.GrayImage(mask_colony)

            threshImageAfterSegment = thresholdImage(GrayImg, 80)


            labelsImg = distanceImage(threshImageAfterSegment, 10)

            _ , bacteriaCenters, total, bacteriaContours, _ , bacteriaCentersScale, bacteriaContoursScale = ScanObject.CountColoni(labelsImg,GrayImg,image_url,segmentcontours,image)
            
            print("----", len(bacteriaContoursScale))

            parsedStafulPoints = []

            for p in statefulPointList:
 
                mouseX = int(p['mouseX'] * 3.5 / 3.6) # *6.5 vi ti le tu canvas len anh goc, / 2 vi giam anh goc di cho nhe segment
                mouseY = int(p['mouseY'] * 3.5 / 3.55)

                parsedStafulPoints.append((mouseX, mouseY, p["action"]))

            for point in parsedStafulPoints:
                print(point)
                if point[2] == "delete":
                    minDist = 100000
                    minIndex = 0

                    for index, (center) in enumerate(bacteriaCenters):
                        dist = math.sqrt( (point[0] - center[0])** 2 + (point[1] -center[1])** 2 )   
                        if dist < minDist: 
                            minDist = dist
                            minIndex = index


                    print("mindist",minDist)
                    if minDist > 12:
                        continue
                    #minBacteria minest!
                    del bacteriaCenters[minIndex]
                    del bacteriaContours[minIndex]

                elif point[2] == "add":
                    bacteriaCenters.append((point[0],point[1]))
                    
                    contourPoints = []
                    for xIndex in range (-4, 6):
                        for yIndex in range(-4, 6):
                            contourPoints.append([point[0]+xIndex, point[1]+yIndex])

                    newContour = np.array(contourPoints).reshape((-1,1,2)).astype(np.int32)

                    newContour = np.array(newContour *  [[3.6,3.55]])
                    newContour =  np.round(newContour).astype(int)

                    bacteriaContours.append(newContour)



            for i in range(0, len(bacteriaCenters)):
                #cv2.putText(image_url, str(i), (bacteriaCenters[i][0], bacteriaCenters[i][1]), cv2.FONT_HERSHEY_SIMPLEX, 0.3, (0,0,255), 1)
                cv2.putText(image,str(i),( int(bacteriaCenters[i][0]*3.6) , int(bacteriaCenters[i][1]* 3.55)), cv2.FONT_HERSHEY_SIMPLEX, 1.1, (0,0,255), 2)
            #cv2.drawContours(image_url, bacteriaContours, -1, (255,0,0), 1)
            cv2.drawContours(image, bacteriaContours,-1, (255,0,0), 1)
            
            cv2.imwrite("image_after_delete_scale.jpg",image)
            cv2.imwrite("image_after_delete.jpg",image_url)
            print("finish written image delete")

            print("bacteriacenters",len(bacteriaCenters) )
            total = len(bacteriaCenters)

            jsonResponse_processpoint = {
                "total": str(total) ,
                "image": os.getcwd() + "\\image_after_delete_scale.jpg"
            }
            jsonResponse_processpoint = "$START$" + json.dumps(jsonResponse_processpoint) + "$END$"

            win32file.WriteFile(fileHandle, bytes(jsonResponse_processpoint,"UTF-8"),None)


            #outputImageAfterDeleteUrl = "$START$" + os.getcwd() + "\\image_after_delete_scale.jpg" + "$END$"

            #win32file.WriteFile(fileHandle, bytes(outputImageAfterDeleteUrl,"UTF-8"),None)


        

#-----------------------------------------------------------------------------------------------------

        elif commandArray[0] == "count":

            print("taked count")

            statefulJsonString = commandArray[1]

            statefulPointList = json.loads(statefulJsonString)

            print(statefulPointList)
          
            image = cv2.imread(commandArray[2])

            image_url = cv2.resize(image, (640,640))
            image_re = cv2.resize(image, (640,640))           
            
            image_v = image_re.copy()

            image_v = cv2.cvtColor(image_re, cv2.COLOR_BGR2HSV).astype("float32")
            v_channel = image_v[:, :, 2]
            image_re[:, :, 0] = v_channel
            image_re[:, :, 1] = v_channel
            image_re[:, :, 2] = v_channel

            image_gray = preprocessing_fn(image_re)

            image_gray = np.transpose(image_gray, (2, 0, 1)).astype("float32")
            

            mask_colony = np.zeros(image_re.shape, dtype='uint8')
 
            x_tensor = torch.from_numpy(image_gray).to(DEVICE).unsqueeze(0)

            pred_mask = best_model(x_tensor)
            pred_mask = pred_mask.detach().squeeze().cpu().numpy()

            pred_mask = np.transpose(pred_mask, (1, 2, 0))

            result_colony = np.where(pred_mask[ :,:, 1] < 0.5, 0, 255).astype('uint8')
            print(result_colony.shape)

            mask_colony[:, :, 0] = result_colony
            mask_colony[:, :, 1] = result_colony
            mask_colony[:, :, 2] = result_colony
  

            GrayImg = ScanObject.GrayImage(mask_colony)

            threshImageAfterSegment = thresholdImage(GrayImg, 80)


            labelsImg = distanceImage(threshImageAfterSegment, 10)

            _ , bacteriaCenters, total, bacteriaContours, _ , bacteriaCentersScale, bacteriaContoursScale = ScanObject.CountColoni(labelsImg,GrayImg,image_url,segmentcontours,image)
        

            parsedStafulPoints = []

            for p in statefulPointList:
 
                mouseX = int(p['mouseX'] * 3.5 / 3.6) # *6.5 vi ti le tu canvas len anh goc, / 2 vi giam anh goc di cho nhe segment
                mouseY = int(p['mouseY'] * 3.5 / 3.55)

                parsedStafulPoints.append((mouseX, mouseY, p["action"]))

            for point in parsedStafulPoints:

                if point[2] == "delete":
                    minDist = 100000
                    minIndex = 0

                    for index, (center) in enumerate(bacteriaCenters):
                        dist = math.sqrt( (point[0] - center[0])** 2 + (point[1] -center[1])** 2 )   
                        if dist < minDist: 
                            minDist = dist
                            minIndex = index

      
                    if minDist > 12:
                        continue
                    #minBacteria minest!
                    del bacteriaCenters[minIndex]
                    del bacteriaContours[minIndex]

                elif point[2] == "add":
                    bacteriaCenters.append((point[0],point[1]))
                    
                    contourPoints = []
                    for xIndex in range (-3, 4):
                        for yIndex in range(-3, 4):
                            contourPoints.append([point[0]+xIndex, point[1]+yIndex])

                    newContour = np.array(contourPoints).reshape((-1,1,2)).astype(np.int32)
             

                    newContour = np.array(newContour *  [[3.6,3.55]])
                    newContour =  np.round(newContour).astype(int)

                    bacteriaContours.append(newContour)


            for i in range(0, len(bacteriaCenters)):
                cv2.putText(image_url, str(i), (bacteriaCenters[i][0], bacteriaCenters[i][1]), cv2.FONT_HERSHEY_SIMPLEX, 0.3, (0,0,255), 1)
                cv2.putText(image,str(i),( int(bacteriaCenters[i][0]*3.6) , int(bacteriaCenters[i][1]* 3.55)), cv2.FONT_HERSHEY_SIMPLEX, 1.1, (0,0,255), 2)
            #cv2.drawContours(image_url, bacteriaContours, -1, (255,0,0), 1)
            cv2.drawContours(image, bacteriaContours,-1, (255,0,0), 1)
            

            
            totalunique = len(bacteriaCenters)
          
            outputTotal = "$START$" + str(totalunique) + "$END$"


            win32file.WriteFile(fileHandle,bytes(outputTotal, "UTF-8"),None)




time.sleep(0.5)



main()


