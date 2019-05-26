# PassportApp 
This is solution folder which contains:
1. PassportService - wcf project which uses my implementation of jmrtd project (mrtd) and allows to read chip data from passports. 
It also contains OCR mechanism based on PassportEye (and Tesseract-OCR) project.
2. DocReader is UWP project created with Windows Template Studio. It communicates with PassportService to do RFID reading and do OCR of passport
3. PassportServiceSetup - setup project for installing some prerequisites
