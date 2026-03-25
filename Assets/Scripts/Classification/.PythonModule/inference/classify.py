import numpy as np
from utils import timed_function

@timed_function
def classify(X_input, classifier):
    probs = classifier.predict_proba(X_input)
    num_rows = len(probs)

    weights = np.ones(num_rows)
    # define weights for some of the last elements
    weights[-1] = 2.0
    avg_probs = np.average(probs, axis=0, weights=weights)

    print("--- Class Probabilities ---")
    for cls, prob in zip(classifier.classes_, avg_probs):
        print(f"Class: {cls} | Probability: {round(float(prob), 3)}")
    print("---------------------------")
    
    max_idx = np.argmax(avg_probs)
    max_class = classifier.classes_[max_idx]
    max_probs = float(round(avg_probs[max_idx], 3))
    
    return max_class, max_probs