import logging
import time

class create_log():
    def __init__(self):
        print("Create logging file")
        logfile="log_report.log"
        file_handler = logging.FileHandler(logfile)
        file_handler.setLevel(logging.DEBUG)
        file_handler.setFormatter(logging.Formatter(
            '%(asctime)s %(pathname)s [%(process)d]: %(levelname)s:: %(message)s'))

        self.logger = logging.getLogger('wbs-server-log')
        self.logger.setLevel(logging.DEBUG)
        self.logger.addHandler(file_handler) 

        self.logger.info("Author: bpham")
        self.logger.info("Create: " + str(time.strftime("%Y%m%d-%H%M%S")))

    def print_log(self, msg, *args, **kwargs):
        self.logger.info(msg)
    
    def print_error(self, msg, *args, **kwargs):
        self.logger.error(msg)
    
    def __exit__(self):
        print("Exit logging")