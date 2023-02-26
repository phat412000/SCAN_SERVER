from scanClass import ScanClass
import cv2

import numpy as np

import websockets
import asyncio


ScanObject= ScanClass()
# img = ScanObject.LoadImage(r"C:\Users\admin\Desktop\do_an_scan\Image file\real_5.png")
# roi = ScanObject.DetectAndTrimDisk(img,20)
# roiandgray = ScanObject.RoiAndGray(roi)
# threshImg = ScanObject.ThreshImage(roiandgray,100)
# labelsImg = ScanObject.LocalImage(threshImg,10)
# finalImage, bacteriaColonies, total = ScanObject.CountColoni(labelsImg,roiandgray,roi)
# cv2.imshow("final image", finalImage)
# cv2.waitKey(0)
#cv2.imshow("final1", threshImg1)
# print(bacteriaColonies)
# cv2.waitKey(0)
# cv2.waitKey(0)

clients = set()  #set = *array ma phan tu khong trung nhau 

def thresholdImage(image,threshValue):
    roiandgray = ScanObject.RoiAndGray(image)
    threshImg = ScanObject.ThreshImage(roiandgray,threshValue)


    return threshImg

    
def distanceImage(image,threshValue, dinstaceValue):
    roiandgray = ScanObject.RoiAndGray(image)
    threshImg  = ScanClass.ThreshImage(roiandgray,threshValue)
    labelsImg = ScanObject.LocalImage(threshImg,dinstaceValue)   
    finalImage = ScanObject.CountColoni(labelsImg,roiandgray,image)

    return finalImage

def countColonies(image,threshValue, distanceValue):
    roiandgray = ScanObject.RoiAndGray(image)
    threshImg = ScanObject.ThreshImage(roiandgray,threshValue)
    labelsImg = ScanObject.LocalImage(threshImg,distanceValue)

    total = ScanObject.CountColoni(labelsImg,roiandgray,image)

    return total

async def processData(message, websocket):

    if message == 'cmdimg':
        imgbuffer = await websocket.recv()
        numpyArray = np.frombuffer(imgbuffer, np.uint8) #mang so cua numpy
        image = cv2.imdecode(numpyArray, cv2.IMREAD_COLOR) #MAT
        
        finalImage,_,_ = countColonies(image)
       
        _, img_buf_arr = cv2.imencode(".png", finalImage)
        cv2.imwrite("asd.jpg",finalImage)

        imageBufferToArray = img_buf_arr.tobytes()

        print(len(imageBufferToArray))
        
        await websocket.send(imageBufferToArray)

        return

    if message == 'cmdcount':
        imgbuffer = await websocket.recv()
        numpyArray = np.frombuffer(imgbuffer, np.uint8) #mang so cua numpy
        image = cv2.imdecode(numpyArray, cv2.IMREAD_COLOR)
        _,_,total = countColonies(image)
        await websocket.send(total)

        return
    
    #cmdthresh_60
    if 'cmdthresh_' in message:
       threshStr = message.split("_")
       threshValue = int(threshStr[1])

       imgbuffer = await websocket.recv()
       numpyArray = np.frombuffer(imgbuffer, np.uint8) #mang so cua numpy
       image = cv2.imdecode(numpyArray, cv2.IMREAD_COLOR) #MAT

       imgThresh = thresholdImage(image, threshValue)
       _, img_buf_arr = cv2.imencode(".png", imgThresh)
       
       cv2.imwrite('thresh.png', imgThresh)

       imageBufferToArray = img_buf_arr.tobytes()

       print(len(imageBufferToArray))
        
       await websocket.send(imageBufferToArray)

       return

    if "cmdsens_" in message:
        SensStr = message.split("_")
        threshValue = int(SensStr[1])
        SensValue   = int(SensStr[2])
        
        imgbuffer = await websocket.recv()
        numpyArray = np.frombuffer(imgbuffer, cv2.IMREAD_COLOR)
        image = cv2.imdecode(numpyArray,cv2.IMREAD_COLOR)
        imgSens = distanceImage(image,threshValue,SensValue)
        _,img_buf_arr = cv2.imencode(".png", imgSens)
        cv2.imwrite("distance.png",imgSens)
        imageBufferToArray = img_buf_arr.tobytes()
        await websocket.send(imageBufferToArray)

        return

     

#738054 - 737280 = 774
#
                                                                           



#vua ket noi den server lan dau tien
async def handler(websocket): 
    clients.add(websocket) #them nguoi dung vao mang

    print("a client connected")

    #dong thoi cung tao ham lap vo tan de lang nghe du lieu nguoi dung
    while True:
        message = await websocket.recv() 
        print(message)

        await processData(message, websocket)
        
        
        #await websocket.send(processedData)


async def looping_forever():
    while True:
        await asyncio.sleep(1)
    


async def main():
    async with websockets.serve(handler, "", 8001, max_size=2**26):
        await looping_forever()


if __name__ == "__main__":
    asyncio.run(main())