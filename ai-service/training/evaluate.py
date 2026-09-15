"""
Model evaluation script for assessing the trained predictive maintenance model.
"""

import sys
from pathlib import Path

ROOT_DIR = Path(__file__).resolve().parent.parent.parent
AI_SERVICE_DIR = ROOT_DIR / "ai-service"
if str(AI_SERVICE_DIR) not in sys.path:
    sys.path.insert(0, str(AI_SERVICE_DIR))

import joblib
import pandas as pd
from sklearn.metrics import classification_report, confusion_matrix, roc_auc_score
from app.features import FeatureEngineer

DATA_PATH = ROOT_DIR / "data" / "synthetic" / "smartfactory_predictive_maintenance_synthetic.csv"
MODEL_FILE = ROOT_DIR / "ai-service" / "models" / "predictive_maintenance_model.joblib"
METADATA_FILE = ROOT_DIR / "ai-service" / "models" / "model_metadata.json"

RAW_FEATURE_COLUMNS = [
    "MachineCategory",
    "AirTemperature_C",
    "ProcessTemperature_C",
    "RotationalSpeed_RPM",
    "Torque_Nm",
    "ToolWear_Min",
]
TARGET_COLUMN = "FailureWithin24h"


def evaluate():
    if not MODEL_FILE.exists():
        raise FileNotFoundError(f"Model file not found at {MODEL_FILE}. Run train.py first.")

    print(f"Loading model from {MODEL_FILE}...")
    pipeline = joblib.load(MODEL_FILE)

    print(f"Loading data from {DATA_PATH}...")
    df = pd.read_csv(DATA_PATH)
    X = df[RAW_FEATURE_COLUMNS]
    y = df[TARGET_COLUMN].astype(int)

    y_pred = pipeline.predict(X)
    y_proba = pipeline.predict_proba(X)[:, 1]

    roc_auc = roc_auc_score(y, y_proba)
    cm = confusion_matrix(y, y_pred)

    print("\n" + "=" * 50)
    print("Full Dataset Evaluation Summary")
    print("=" * 50)
    print(f"Overall ROC-AUC Score: {roc_auc:.4f}")
    print("\nConfusion Matrix:")
    print(f"TN: {cm[0][0]} | FP: {cm[0][1]}")
    print(f"FN: {cm[1][0]} | TP: {cm[1][1]}")
    print("\nDetailed Classification Report:\n")
    print(classification_report(y, y_pred, target_names=["Normal", "Failure"]))


if __name__ == "__main__":
    evaluate()
