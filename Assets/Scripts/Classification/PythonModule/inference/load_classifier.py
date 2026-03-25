import joblib
import os
from typing import Optional, Any

def load_classifier_from_joblib(filename: str, models_folder: str = "models") -> Optional[Any]:
    # Ensure .joblib extension
    if not filename.endswith('.joblib'):
        filename += '.joblib'
    
    # Construct full path
    if not filename.startswith(models_folder) and not filename.startswith(f".\\{models_folder}") :
        filepath = os.path.join(models_folder, filename)
    else:
        filepath = filename
    
    try:
        if not os.path.exists(filepath):
            print(f"❌ File not found: {filepath}")
            return None
        
        classifier = joblib.load(filepath)
        print(f"✅ Classifier loaded from: {filepath}")
        
        return classifier
        
    except Exception as e:
        print(f"❌ Error loading classifier: {e}")
        return None


def load_classifier_with_info(filename: str, models_folder: str = "models") -> Optional[Any]:
    classifier = load_classifier_from_joblib(filename, models_folder)
    
    if classifier is not None:
        print("\n📊 Classifier Information:")
        print(f"   - Type: {type(classifier).__name__}")
        
        # Show feature importance if available
        if hasattr(classifier, 'feature_importances_'):
            print(f"   - Feature importances: {classifier.feature_importances_}")
        
        # Show classes if available
        if hasattr(classifier, 'classes_'):
            print(f"   - Classes: {classifier.classes_}")
        
        # Show tree depth for decision trees
        if hasattr(classifier, 'get_depth'):
            print(f"   - Tree depth: {classifier.get_depth()}")
            print(f"   - Number of leaves: {classifier.get_n_leaves()}")
        
        # Show number of estimators for random forests
        if hasattr(classifier, 'n_estimators'):
            print(f"   - Number of trees: {classifier.n_estimators}")
    
    return classifier