from scanClass import ScanClass
import cv2

import numpy as np

import websockets
import asyncio

tam = False

ScanObject = ScanClass()
# img = ScanObject.LoadImage(r"C:\Users\admin\Desktop\do_an_scan\Image file\real_2.png")
# roi = ScanObject.DetectAndTrimDisk(img,20)
# roiandgray = ScanObject.RoiAndGray(roi)
# threshImg = ScanObject.ThreshImage(roiandgray,131)
# labelsImg = ScanObject.LocalImage(threshImg,3)
# FinalImage, bacteriaColonies, total = ScanObject.CountColoni(labelsImg,roiandgray,roi)
# #cv2.imshow("final", FinalImage)
# # print(bacteriaColonies)
# # cv2.waitKey(0)
# print(total)
# cv2.waitKey(0)

clients = set()  #set = *array ma phan tu khong trung nhau 

async def processData(message, websocket):

    if message == 'cmdimg':
        imgbuffer = await websocket.recv()
        print("asdasd")
        numpyArray = np.frombuffer(imgbuffer, np.uint8) #mang so cua numpy
        imageConverter = cv2.imdecode(numpyArray, cv2.IMREAD_COLOR) #MAT
        cv2.imwrite(r'C:\Users\admin\Desktop\do_an_scan\Source_work_image\SourceImage.jpg', imageConverter)
        roi = ScanObject.DetectAndTrimDisk(imageConverter,20)
        roiandgray = ScanObject.RoiAndGray(roi)
        threshImg = ScanObject.ThreshImage(roiandgray,131)
        labelsImg = ScanObject.LocalImage(threshImg,3)
        FinalImage, bacteriaColonies, total = ScanObject.CountColoni(labelsImg,roiandgray,roi)
       
        _, img_buf_arr = cv2.imencode(".jpg", FinalImage)
        cv2.imwrite("asd.jpg",img_buf_arr)
        byte_img = img_buf_arr.tobytes()
        print(len(byte_img))
        await websocket.send(imgbuffer)
        tam = True
        return
    
    if tam == False:
        return
    
    if message == 'cmdcount':
        await websocket.send(total)
     


                                                                           



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