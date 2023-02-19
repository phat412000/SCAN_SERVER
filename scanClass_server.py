from scanClass import ScanClass
import cv2

ScanObject = ScanClass()
img = ScanObject.LoadImage(r"C:\Users\admin\Desktop\do_an_scan\Image file\real_2.png")
roi = ScanObject.DetectAndTrimDisk(img,20)
roiandgray = ScanObject.RoiAndGray(roi)
threshImg = ScanObject.ThreshImage(roiandgray,131)
labelsImg = ScanObject.LocalImage(threshImg,3)
FinalImage, bacteriaColonies = ScanObject.CountColoni(labelsImg,roiandgray,roi)
cv2.imshow("final", FinalImage)
print(bacteriaColonies)
cv2.waitKey(0)