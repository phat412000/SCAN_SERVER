from enum import unique
from math import floor
import cv2
import numpy as np
from skimage.feature import peak_local_max
from skimage.segmentation import watershed
from scipy import ndimage
from collections import Counter
import matplotlib.pyplot as plt
import sys
from PIL import Image, ImageDraw

from sklearn.cluster import KMeans

np.set_printoptions(threshold = sys.maxsize)


MIN_BACTERIA_SIZE = 20   

def detectAndTrimDisk(image, padding_size):
    
    minRadius = 0
    maxRadius = image.shape[1] #image width
    minDist = 100

    gray = cv2.cvtColor(image, cv2.COLOR_BGR2GRAY)

    circles = cv2.HoughCircles(gray, cv2.HOUGH_GRADIENT, 1, minDist=minDist,minRadius=minRadius, maxRadius=maxRadius)

    biggestRadius = 0 
    biggestCenter = (0,0)
    
    if circles is not None:
        circles = np.uint16(np.around(circles))
        for i in circles[0,:]:
            if i[2] > biggestRadius:
                biggestRadius = i[2]
                biggestCenter = (i[0], i[1])
                
    mask = np.zeros_like(image)
    
    cv2.circle(mask, biggestCenter, biggestRadius - padding_size, (255, 255,255), -1) #-1 is fill-up circle 
    #cv2.imshow('image', image)
    roi = cv2.bitwise_and(image, mask)
   
    roi[roi <= 10] = 255

    return roi

image0 = cv2.imread(r"C:\Users\admin\Desktop\do_an_scan\Image file\bacteria_test.jpg")
image = image0.copy()
image = detectAndTrimDisk(image, 20)
cv2.imshow('Cropped Image', image)
gray = cv2.cvtColor(image, cv2.COLOR_BGR2GRAY)
cv2.imshow('gray', gray)


thresh = cv2.threshold(gray, 80, 255, cv2.THRESH_BINARY_INV + cv2.THRESH_OTSU)[1]
distance_map = ndimage.distance_transform_edt(thresh)

local_max = peak_local_max(distance_map, indices=False, min_distance=10) #3

# Perform connected component analysis then apply Watershed
markers = ndimage.label(local_max, structure=np.ones((3, 3)))[0]
labels = watershed(-distance_map, markers, mask=thresh)

countBacteria = 0 
bacteriaColonies = []

bacteriaColors = []

uniqueLabels = np.unique(labels)

print(uniqueLabels)

for label in np.unique(labels):                                                                         
    if label == 0:
        continue

    mask = np.zeros(gray.shape, dtype="uint8")
    mask[labels == label] = 255

    # Find contours and determine contour area
    cnts = cv2.findContours(mask.copy(), cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)
    cnts = cnts[0] if len(cnts) == 2 else cnts[1]
   
    biggestContourInAzone = max(cnts, key=cv2.contourArea)
    
    #find biggest contour in a zone size
    currentContourSize = cv2.contourArea(biggestContourInAzone) 
    
    if currentContourSize < MIN_BACTERIA_SIZE:
       continue
    
    
    #find center of contour using moments
    centerOfContour = cv2.moments(biggestContourInAzone)
    centerX = int(centerOfContour['m10']/centerOfContour['m00'])
    centerY = int(centerOfContour['m01']/centerOfContour['m00'])
    centerOfBacteria = (centerX, centerY)
    print ("colors", image0[centerY,centerX]) #BGR
    
    bacteriaColors.append(image0[centerY, centerX])

    countBacteria += 1
    
    bacteriaObject = { "id": countBacteria, "size": currentContourSize, "position": centerOfBacteria}
    print("bacteria id: " + str(countBacteria) + " size: " + str(currentContourSize))
    
    bacteriaColonies.append(bacteriaObject)
        
    image = cv2.putText(image, str(countBacteria), centerOfBacteria, cv2.FONT_HERSHEY_SIMPLEX, 0.4, (0, 0, 0), 1)
    cv2.drawContours(image, [biggestContourInAzone], -1, (255, 0,0 ), 1)
    
    
kmeans = KMeans(n_clusters=10, random_state=0).fit(bacteriaColors)
print("kmean labels:")
print(len(kmeans.labels_))

bacteriaColorClustered = image0.copy()

for bacteria in bacteriaColonies:
    position = bacteria["position"]
    color = image0[position[1], position[0]]
    kmeanPredict = kmeans.predict([color])[0]

    cv2.putText(bacteriaColorClustered, str(kmeanPredict), position, cv2.FONT_HERSHEY_SIMPLEX, 0.4, (0, 0, 0), 1)
    print(bacteria["position"])
    print(kmeanPredict)

print("total " + str(countBacteria) + " bacteria colony")

cv2.imshow('Color clustering', bacteriaColorClustered)
#fig, axes = plt.subplots(ncols=3, figsize=(9, 3), sharex=True, sharey=True)
#ax = axes.ravel()
#print('num2', contour_num)


#ax[0].imshow(-distance_map, cmap=plt.cm.gray)
#ax[0].set_title('Distances')
#ax[1].imshow(labels)
#ax[1].set_title('labels') 
#plt.show()


cv2.imshow('Final', image)
fig, axes = plt.subplots(ncols=3, figsize=(9, 3), sharex=True, sharey=True)
ax = axes.ravel()
ax[0].imshow(-distance_map, cmap=plt.cm.gray)
ax[0].set_title('Distances')
ax[1].imshow(labels)
ax[1].set_title('labels')
plt.show()
cv2.waitKey(0)
cv2.destroyAllWindows() 

