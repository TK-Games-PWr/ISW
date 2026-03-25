from .status_enums import Status
from utils import is_null_or_whitespace
import json
import sys


'''
Function that generates json response to standard output
'''
def generate_response(status: Status, 
                      player_type: str = "", 
                      error_message: str = "", 
                      probability: float = -1) -> str:
    if(status == status.OK and is_null_or_whitespace(player_type) ):
        raise AttributeError("If status is succesful, player type can't be empty")
    
    print("RESPONSE_DATA:")
    print(
        json.dumps(
            {
                "status": f"{status}",
                "content": f"{player_type}",
                "errorMessage": f"{error_message}",
                "probability": f"{probability}"
            }
        )
    ) 
    sys.stdout.flush()
