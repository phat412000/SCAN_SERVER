from enum import unique
from math import floor
import cv2
import numpy as np
from skimage.feature import peak_local_max
from skimage.segmentation import watershed
from scipy import ndimage
import sys



#np.set_printoptions(threshold = sys.maxsize)

class ScanClass:        

    def DetectAndTrimDisk(self,Img ,padding_size):
        Imgresize = cv2.resize(Img, (600, 410))
        minRadius     = 0
        maxRadius     = Imgresize.shape[1] #image width
        minDist       = 100
        biggestRadius = 0 
        biggestCenter = (0,0)
        gray    = cv2.cvtColor(Imgresize, cv2.COLOR_BGR2GRAY)
        circles = cv2.HoughCircles(gray, cv2.HOUGH_GRADIENT, 1, minDist = minDist,
            minRadius= minRadius,maxRadius= maxRadius) 
        if circles is not None:
            circles = np.uint16(np.around(circles))
            for i in circles[0,:]:
                if i[2] > biggestRadius:
                    biggestRadius = i[2]
                    biggestCenter = (i[0], i[1])

        mask = np.zeros_like(Imgresize)
        cv2.circle(mask, biggestCenter, biggestRadius - padding_size,
            (255,255,255), -1)

        roi = cv2.bitwise_and(Imgresize, mask)
        return roi


    def LoadImage(self, ImgUrl):
        imgOriginal = cv2.imread(ImgUrl)
     
        return imgOriginal


    def RoiAndGray(self, roi):
        roiandgray = cv2.cvtColor(roi,cv2.COLOR_BGR2GRAY)

        return roiandgray

    
    
    def ThreshImage(self,grayandroi,ThreshValue):
        thresh = cv2.threshold(grayandroi, ThreshValue, 255,  
        cv2.THRESH_BINARY_INV )[1]

        return thresh 

    
    def LocalImage(self,threshImg, minDistanceValue):
        distance_map = ndimage.distance_transform_edt(threshImg)
        local_max    = peak_local_max(distance_map, indices=False,
            min_distance=minDistanceValue)
        markers = ndimage.label(local_max, structure=np.ones((3,3)))[0]
        labelsImg  = watershed(-distance_map, markers, mask=threshImg)

        return labelsImg
    
    
    def CountColoni(self,labelsImg,roiandgray,roi):
        MIN_BACTERIA_SIZE = 20
        countBacteria    = 0
        bacteriaColonies = []
        bacteriaCenters  = []

        uniqueLabels = np.unique(labelsImg)
        for label in np.unique(labelsImg):
            if label == 0:
                continue

            mask = np.zeros(roiandgray.shape, dtype="uint8")
            mask[labelsImg == label] = 255

            cnts = cv2.findContours(mask.copy(), cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)
            cnts = cnts[0] if len(cnts) == 2 else cnts[1]

            biggestContourInAzone = max(cnts, key=cv2.contourArea)

            currentContourSize = cv2.contourArea(biggestContourInAzone)

            if currentContourSize < MIN_BACTERIA_SIZE:
                continue

            centerOfContour = cv2.moments(biggestContourInAzone)
            centerX = int(centerOfContour['m10']/centerOfContour['m00'])
            centerY = int(centerOfContour['m01']/centerOfContour['m00'])
            centerOfBacteria = (centerX,centerY)
            bacteriaCenters.append([centerX,centerY])

            countBacteria += 1

            #bacteriaObject = { "bacteria id: ":  countBacteria, " size ": currentContourSize, " position ": centerOfBacteria}
            showed = ("bacteria id: " + str(countBacteria) + " size: " + str(currentContourSize) + " position: " + str(centerOfBacteria) )
            bacteriaColonies.append(centerOfBacteria)
            
            finalmage = cv2.putText(roi,str(countBacteria), centerOfBacteria, cv2.FONT_HERSHEY_SIMPLEX, 0.3, (0,0,255), 1)
            ContoursColonies = cv2.drawContours(finalmage,[biggestContourInAzone], -1, (255,0,0), 1)

        return finalmage, bacteriaCenters, countBacteria    
    
