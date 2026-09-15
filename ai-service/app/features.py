import numpy as np
import pandas as pd
from sklearn.base import BaseEstimator, TransformerMixin

class FeatureEngineer(BaseEstimator, TransformerMixin):
    """Calculates domain-specific industrial features like temperature difference and mechanical power."""

    def fit(self, X, y=None):
        return self

    def transform(self, X):
        X_df = pd.DataFrame(X).copy()
        X_df["TempDiff"] = X_df["ProcessTemperature_C"] - X_df["AirTemperature_C"]
        X_df["Power_W"] = (
            X_df["RotationalSpeed_RPM"] * X_df["Torque_Nm"] * (2 * np.pi / 60)
        )
        return X_df
