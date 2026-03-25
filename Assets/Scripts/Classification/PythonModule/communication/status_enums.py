from enum import Enum

# can be expanded if more cases arise

class Status(Enum):
    ERROR=0
    OK=1

    def __str__(self):
        return self.name