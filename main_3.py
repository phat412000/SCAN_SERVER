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
OFFSET_BETWEEN_CLUSTER = 5 #[H]ue distance 
DEFAULT_CLUSTER_NUMBER = 7 #7 basic colors

def estimate_cluster_number_by_offset(kmeans):
    unique_clusters = []
    
    total_cluster_center_number = len(kmeans.cluster_centers_)
    print ("Kmeans ID: ", kmeans.cluster_centers_)
    checkpoints = [False] * total_cluster_center_number
    
    for i in range(total_cluster_center_number):

        if checkpoints[i]:
            continue  #true

        checkpoints[i] = True

        current_cluster_center = kmeans.cluster_centers_[i] #vitri
        unique_clusters.append(current_cluster_center)

        for j in range(total_cluster_center_number):
            checking_cluster_center = kmeans.cluster_centers_[j]

            if checkpoints[j]:
                continue

            dist = np.linalg.norm(current_cluster_center - checking_cluster_center)
            if dist <= OFFSET_BETWEEN_CLUSTER:
                checkpoints[j] = True 
     
    return len(unique_clusters)

def KMeansFunction(BacteriaHsvColors): 
    global OFFSET_BETWEEN_CLUSTER, DEFAULT_CLUSTER_NUMBER

    kmeans = KMeans(n_clusters=DEFAULT_CLUSTER_NUMBER, random_state=0).fit(BacteriaHsvColors)

    valid_cluster_number = estimate_cluster_number_by_offset(kmeans)
    print("estimate valid cluster number:")
    print(valid_cluster_number)

    kmeans = KMeans(n_clusters=valid_cluster_number, random_state=0).fit(BacteriaHsvColors)
    
    pred_label = kmeans.predict(BacteriaHsvColors)
    return pred_label

def kmeans_display(X, label):
    K = np.amax(label) + 1

    plt.plot(X[:, 0], X[:, 1], 'b^', markersize=4, alpha=.8)

    plt.axis('equal')
    plt.plot()
    plt.show() 

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
    # cv2.imshow('image', image
    roi = cv2.bitwise_and(image, mask)
   
    roi[roi <= 10] = 255

    return roi

image0 = cv2.imread(r"C:\Users\admin\Desktop\do_an_scan\Image file\bacteria_test_1.jpg")
image = image0.copy()
image1 = image0.copy()
cv2.imshow('Cropped Image1', image)
image = detectAndTrimDisk(image, 20)
cv2.imshow('TrimDisk Image', image)
gray = cv2.cvtColor(image, cv2.COLOR_BGR2GRAY)
Hsv = cv2.cvtColor(image1, cv2.COLOR_BGR2HSV)
#cv2.imshow('gray', gray)
#cv2.imshow("HSV", Hsv)


thresh = cv2.threshold(gray, 80, 255, cv2.THRESH_BINARY_INV + cv2.THRESH_OTSU)[1]
distance_map = ndimage.distance_transform_edt(thresh)

local_max = peak_local_max(distance_map, indices=False, min_distance=10) #3

# Perform connected component analysis then apply Watershed
markers = ndimage.label(local_max, structure=np.ones((3, 3)))[0]
labels = watershed(-distance_map, markers, mask=thresh)

countBacteria     = 0 
bacteriaColonies  = []
BacteriaHsvColors = []
bacteriaCenters   = []


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
    centerOfBacteria = (centerX,centerY)
    bacteriaCenters.append([centerX,centerY])
    #print ("colors", image0[centerY,centerX]) #BGR


    countBacteria += 1
    
    bacteriaObject = { "id": countBacteria, "size": currentContourSize, "position": centerOfBacteria}
    print("bacteria id: " + str(countBacteria) + " size: " + str(currentContourSize) + "position: " + str(centerOfBacteria) )
    
    bacteriaColonies.append(bacteriaObject)
        
    image = cv2.putText(image, str(countBacteria), centerOfBacteria , cv2.FONT_HERSHEY_SIMPLEX, 0.4, (0, 0, 0), 1)
    cv2.drawContours(image, [biggestContourInAzone], -1, (255, 0,0 ), 1)

    InputKmeans = [Hsv[centerY][centerX][0], 0]
    BacteriaHsvColors.append(InputKmeans)

pred_label = KMeansFunction(BacteriaHsvColors)
print ("test", pred_label)

AmountColors = np.array(pred_label)
unique, counts = np.unique(AmountColors, return_counts = True)
print(dict(zip(unique, counts)))


for c, i in zip(bacteriaCenters, pred_label):
    if(i != 0):
        cv2.putText(image1, str(i), (c[0], c[1]), cv2.FONT_HERSHEY_SIMPLEX, 0.5, (255, 0, 0), 2)
    else:
        cv2.putText(image1, str(i), (c[0], c[1]), cv2.FONT_HERSHEY_SIMPLEX, 0.5, (0, 0, 0), 1)
    # cv2.circle(image, (c[1], c[0]), 3, color=(255, 0, int(int(i)*10 + 100)), thickness=3)
cv2.imshow("dhk", image1)

kmeans_display(np.array(BacteriaHsvColors), pred_label)


print("total " + str(countBacteria) + " bacteria colony")

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

