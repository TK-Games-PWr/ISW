from communication.communication import generate_response
from communication.status_enums import Status
from inference.load_classifier import *
from inference.classify import *

from pydantic import BaseModel, Field, field_validator
from pathlib import Path
from typing import Optional
import argparse
import pandas as pd
import time
        


class Arguments(BaseModel):
    datapoints_file: str
    decision_tree_parameters: str
    
    @field_validator('datapoints_file')
    def validate_datapoints(cls, v):
        path = Path(v)
        if not path.exists():
            raise ValueError(f"File does not exist: {v}")
        if not path.is_file():
            raise ValueError(f"Not a file: {v}")
        if path.suffix.lower() not in ['.xlsx', '.csv']:
            raise ValueError(f"Must be .xlsx or .csv file: {v}")
        return v
    
    @field_validator('decision_tree_parameters')
    def validate_parameters(cls, v):
        path = Path(v)
        if not path.exists():
            raise ValueError(f"File does not exist: {v}")
        if not path.is_file():
            raise ValueError(f"Not a file: {v}")
        if path.suffix.lower() not in ['.pkl', '.joblib']:
            raise ValueError(f"Must be .pkl or .joblib file")
        return v


def get_arguments() -> Arguments:
    parser = argparse.ArgumentParser(
        description='Process decision tree with data from Excel file',
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog='Example: %(prog)s data.xlsx params.joblib'
    )
    
    parser.add_argument(
        'datapoints_file',
        type=str,
        help='Path to Excel file (.xlsx or .csv) containing datapoints'
    )
    
    parser.add_argument(
        'decision_tree_parameters',
        type=str,
        help='Path to file containing decision tree parameters'
    )
    
    args = parser.parse_args()
    
    return Arguments(
        datapoints_file=args.datapoints_file,
        decision_tree_parameters=args.decision_tree_parameters
    )


def load_datapoints(file_path: str):
    file_extension = Path(file_path).suffix.lower()
    
    to_omit = ['timestamp'] 

    if file_extension == '.csv':
        try:
            df = pd.read_csv(file_path, sep=';', header=0)
        except:
            df = pd.read_csv(file_path, sep=',', header=0)
    elif file_extension in ['.xlsx', '.xls']:
        df = pd.read_excel(file_path, engine='openpyxl', header=0)
    else:
        raise ValueError(f"Unsupported file format: {file_extension}")

    df.columns = [c.strip() for c in df.columns]
    df = df.drop(columns=to_omit, errors='ignore')

    return df


def safe_load_datapoints(file_path, max_retries=5, delay=0.2):
    """
    Attempts to read a file, retrying if it's locked by another process
    """
    for i in range(max_retries):
        try:
            with open(file_path, 'r'):
                return load_datapoints(file_path)
        except (IOError, PermissionError):
            if i < max_retries - 1:
                time.sleep(delay)
                continue
            else:
                raise


def main():
    print("Main process started")
    try:
        args = get_arguments()

        classifier = load_classifier_with_info(args.decision_tree_parameters)

        X_input = safe_load_datapoints(args.datapoints_file)

        max_class, max_probability = classify(X_input, classifier)
        generate_response(Status.OK, player_type=max_class, probability=max_probability)

    except Exception as e:
        generate_response(Status.ERROR, error_message=str(e))

if __name__ == "__main__":
    main()