import sys
from pathlib import Path

ROOT_DIR = Path(__file__).resolve().parent.parent
if str(ROOT_DIR) not in sys.path:
    sys.path.insert(0, str(ROOT_DIR))

import pytest
from fastapi.testclient import TestClient
from app.main import app


@pytest.fixture
def client():
    with TestClient(app) as test_client:
        yield test_client


def test_health_check(client):
    response = client.get("/health")
    assert response.status_code == 200
    data = response.json()
    assert data["status"] == "healthy"
    assert data["model_loaded"] is True
    assert data["model_type"] == "RandomForestClassifier"


def test_model_metadata(client):
    response = client.get("/model/metadata")
    assert response.status_code == 200
    data = response.json()
    assert data["model_type"] == "RandomForestClassifier"
    assert "metrics" in data
    assert "risk_thresholds" in data


def test_predict_single_normal(client):
    payload = {
        "machine_id": "MCH-001",
        "machine_category": "LightDuty",
        "air_temperature_c": 22.0,
        "process_temperature_c": 32.0,
        "rotational_speed_rpm": 1500.0,
        "torque_nm": 35.0,
        "tool_wear_min": 10.0,
    }
    response = client.post("/predict", json=payload)
    assert response.status_code == 200
    data = response.json()
    assert data["machine_id"] == "MCH-001"
    assert 0.0 <= data["failure_probability"] <= 1.0
    assert data["risk_level"] in ("Düşük", "Orta", "Yüksek", "Kritik")
    assert "recommendation" in data
    assert isinstance(data["failure_predicted"], bool)


def test_predict_single_high_stress(client):
    # Elevated temperatures, high speed and torque with heavy tool wear
    payload = {
        "machine_id": "MCH-099",
        "machine_category": "HeavyDuty",
        "air_temperature_c": 31.0,
        "process_temperature_c": 50.0,
        "rotational_speed_rpm": 2800.0,
        "torque_nm": 82.0,
        "tool_wear_min": 180.0,
    }
    response = client.post("/predict", json=payload)
    assert response.status_code == 200
    data = response.json()
    assert data["machine_id"] == "MCH-099"
    assert data["failure_probability"] > 0.0


def test_predict_batch(client):
    payload = {
        "items": [
            {
                "machine_id": "MCH-101",
                "machine_category": "MediumDuty",
                "air_temperature_c": 21.5,
                "process_temperature_c": 35.0,
                "rotational_speed_rpm": 1400.0,
                "torque_nm": 38.0,
                "tool_wear_min": 25.0,
            },
            {
                "machine_id": "MCH-102",
                "machine_category": "HeavyDuty",
                "air_temperature_c": 29.0,
                "process_temperature_c": 48.0,
                "rotational_speed_rpm": 2600.0,
                "torque_nm": 75.0,
                "tool_wear_min": 150.0,
            },
        ]
    }
    response = client.post("/predict/batch", json=payload)
    assert response.status_code == 200
    data = response.json()
    assert data["total_processed"] == 2
    assert len(data["predictions"]) == 2
    assert "high_risk_count" in data


def test_predict_invalid_input_validation(client):
    # Negative rotational speed and out of bounds temperature
    payload = {
        "machine_id": "MCH-ERR",
        "machine_category": "MediumDuty",
        "air_temperature_c": -100.0,  # ge=-50
        "process_temperature_c": 35.0,
        "rotational_speed_rpm": -50.0,  # ge=0
        "torque_nm": 38.0,
        "tool_wear_min": 25.0,
    }
    response = client.post("/predict", json=payload)
    assert response.status_code == 422  # Unprocessable Entity
