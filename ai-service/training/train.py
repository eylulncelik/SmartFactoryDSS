"""
Model training pipeline for SmartFactoryDSS predictive maintenance failure-risk estimation.

Rule compliance:
- Features DO NOT include MachineId, Timestamp, FailureType, or FailureWithin24h.
- Target is FailureWithin24h (binary).
"""

import json
import os
import sys
from datetime import datetime, timezone
from pathlib import Path

# Base paths
ROOT_DIR = Path(__file__).resolve().parent.parent.parent
AI_SERVICE_DIR = ROOT_DIR / "ai-service"
if str(AI_SERVICE_DIR) not in sys.path:
    sys.path.insert(0, str(AI_SERVICE_DIR))

import joblib
import numpy as np
import pandas as pd
from sklearn.compose import ColumnTransformer
from sklearn.ensemble import RandomForestClassifier
from sklearn.metrics import (
    accuracy_score,
    classification_report,
    f1_score,
    precision_score,
    recall_score,
    roc_auc_score,
)
from sklearn.model_selection import train_test_split
from sklearn.pipeline import Pipeline
from sklearn.preprocessing import OneHotEncoder, StandardScaler

from app.features import FeatureEngineer

DATA_PATH = ROOT_DIR / "data" / "synthetic" / "smartfactory_predictive_maintenance_synthetic.csv"
MODEL_DIR = ROOT_DIR / "ai-service" / "models"
MODEL_FILE = MODEL_DIR / "predictive_maintenance_model.joblib"
METADATA_FILE = MODEL_DIR / "model_metadata.json"

RAW_FEATURE_COLUMNS = [
    "MachineCategory",
    "AirTemperature_C",
    "ProcessTemperature_C",
    "RotationalSpeed_RPM",
    "Torque_Nm",
    "ToolWear_Min",
]

TARGET_COLUMN = "FailureWithin24h"


def train_model():
    print(f"Loading synthetic dataset from {DATA_PATH}...")
    if not DATA_PATH.exists():
        raise FileNotFoundError(f"Dataset not found at {DATA_PATH}")

    df = pd.read_csv(DATA_PATH)
    print(f"Dataset loaded: {df.shape[0]} rows, {df.shape[1]} columns.")

    # Validate required columns
    missing_cols = set(RAW_FEATURE_COLUMNS + [TARGET_COLUMN]) - set(df.columns)
    if missing_cols:
        raise ValueError(f"Missing expected columns in dataset: {missing_cols}")

    X = df[RAW_FEATURE_COLUMNS].copy()
    y = df[TARGET_COLUMN].astype(int)

    print(f"Target distribution: {np.bincount(y)} (Failure rate: {y.mean():.2%})")

    # Stratified Train/Test split (80/20)
    X_train, X_test, y_train, y_test = train_test_split(
        X, y, test_size=0.20, random_state=42, stratify=y
    )

    numeric_features = [
        "AirTemperature_C",
        "ProcessTemperature_C",
        "RotationalSpeed_RPM",
        "Torque_Nm",
        "ToolWear_Min",
        "TempDiff",
        "Power_W",
    ]
    categorical_features = ["MachineCategory"]

    column_transformer = ColumnTransformer(
        transformers=[
            ("num", StandardScaler(), numeric_features),
            (
                "cat",
                OneHotEncoder(handle_unknown="ignore", sparse_output=False),
                categorical_features,
            ),
        ]
    )

    pipeline = Pipeline(
        steps=[
            ("engineer", FeatureEngineer()),
            ("preprocessor", column_transformer),
            (
                "classifier",
                RandomForestClassifier(
                    n_estimators=120,
                    max_depth=12,
                    min_samples_split=4,
                    min_samples_leaf=2,
                    class_weight="balanced",
                    random_state=42,
                    n_jobs=-1,
                ),
            ),
        ]
    )

    print("Training RandomForest model pipeline...")
    pipeline.fit(X_train, y_train)

    print("Evaluating model performance on test dataset...")
    y_pred = pipeline.predict(X_test)
    y_proba = pipeline.predict_proba(X_test)[:, 1]

    roc_auc = float(roc_auc_score(y_test, y_proba))
    precision = float(precision_score(y_test, y_pred))
    recall = float(recall_score(y_test, y_pred))
    f1 = float(f1_score(y_test, y_pred))
    accuracy = float(accuracy_score(y_test, y_pred))

    print("\n--- Test Set Evaluation Metrics ---")
    print(f"ROC-AUC Score : {roc_auc:.4f}")
    print(f"Precision     : {precision:.4f}")
    print(f"Recall        : {recall:.4f}")
    print(f"F1 Score      : {f1:.4f}")
    print(f"Accuracy      : {accuracy:.4f}")
    print("\nClassification Report:\n", classification_report(y_test, y_pred))

    # Save model artifact
    MODEL_DIR.mkdir(parents=True, exist_ok=True)
    joblib.dump(pipeline, MODEL_FILE)
    print(f"Model saved successfully to {MODEL_FILE}")

    # Save metadata
    metadata = {
        "model_type": "RandomForestClassifier",
        "trained_at": datetime.now(timezone.utc).isoformat(),
        "dataset_rows": len(df),
        "test_size": len(X_test),
        "metrics": {
            "roc_auc": round(roc_auc, 4),
            "precision": round(precision, 4),
            "recall": round(recall, 4),
            "f1_score": round(f1, 4),
            "accuracy": round(accuracy, 4),
        },
        "raw_features": RAW_FEATURE_COLUMNS,
        "engineered_features": ["TempDiff", "Power_W"],
        "target": TARGET_COLUMN,
        "risk_thresholds": {
            "low_max": 0.30,
            "medium_max": 0.60,
            "high_max": 0.80,
        },
    }

    with open(METADATA_FILE, "w", encoding="utf-8") as f:
        json.dump(metadata, f, indent=2)
    print(f"Metadata saved successfully to {METADATA_FILE}")

    return metadata


if __name__ == "__main__":
    train_model()
