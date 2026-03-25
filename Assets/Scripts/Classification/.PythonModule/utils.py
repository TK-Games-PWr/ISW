from time import time
from functools import wraps

def is_null_or_whitespace(s: str):
    return s is None or len(s.strip()) == 0


def timed_function(func):
    @wraps(func)
    def wrapper(*args, **kwargs):
        start_time = time()
        result = func(*args, **kwargs)
        end_time = time()
        print(f"{func.__name__} took {(end_time - start_time):.4f} seconds")
        return result
    return wrapper
