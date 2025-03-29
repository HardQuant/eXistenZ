import pandas as pd
import pyodbc
import re
from datetime import datetime

# Database connection
DB_CONNECTION_STRING = (
    r'DRIVER={ODBC Driver 17 for SQL Server};'
    r'SERVER=DESKTOP-FIQB2SM\SQLEXPRESS;'
    r'DATABASE=PsychGroups;'
    r'Trusted_Connection=yes;'
)

LOG_FILE = "diagnosis_etl_log.txt"

def log_message(message):
    with open(LOG_FILE, "a") as log:
        log.write(f"[{datetime.now()}] {message}\n")

def get_facility_guid(cursor, filename):
    match = re.match(r"(.*?)_PatientData_", filename)
    facility_name_fragment = match.group(1).replace("_", " ") if match else None

    cursor.execute("SELECT FacilityGUID, Name FROM Facilities")
    facilities = cursor.fetchall()

    fragment_clean = facility_name_fragment.lower().replace(" ", "")
    for guid, name in facilities:
        name_clean = name.lower().replace(" ", "")
        if fragment_clean in name_clean:
            return guid

    raise ValueError(f"Facility name like '{facility_name_fragment}' not found in Facilities table.")

def load_valid_diagnosis_codes(cursor):
    cursor.execute("SELECT Dx_Code FROM Diagnoses")
    return set(row[0] for row in cursor.fetchall())

def load_diagnoses(csv_path):
    conn = pyodbc.connect(DB_CONNECTION_STRING)
    cursor = conn.cursor()

    filename = csv_path.split("\\")[-1]
    facility_guid = get_facility_guid(cursor, filename)
    valid_codes = load_valid_diagnosis_codes(cursor)

    df = pd.read_csv(csv_path)
    df['First_Name'] = df['First_Name'].astype(str).str.strip()
    df['Last_Name'] = df['Last_Name'].astype(str).str.strip()
    df['PSYCH DX'] = df['PSYCH DX'].astype(str)

    inserted = 0
    skipped = 0
    invalid = 0
    missing_patients = 0

    for _, row in df.iterrows():
        first_name = row['First_Name']
        last_name = row['Last_Name']
        dx_codes = [code.strip() for code in row['PSYCH DX'].split(',') if code.strip()]

        # Look up PatientGUID
        cursor.execute("""
            SELECT PatientGUID FROM Patient
            WHERE First_Name = ? AND Last_Name = ? AND FacilityGUID = ?
        """, first_name, last_name, facility_guid)
        result = cursor.fetchone()

        if not result:
            log_message(f"Missing patient: {first_name} {last_name}")
            missing_patients += 1
            continue

        patient_guid = result[0]

        for dx in dx_codes:
            if dx not in valid_codes:
                log_message(f"Invalid Dx_Code '{dx}' for {first_name} {last_name}")
                invalid += 1
                continue

            cursor.execute("""
                SELECT 1 FROM Patient_Diagnoses WHERE PatientGuid = ? AND Dx_Code = ?
            """, patient_guid, dx)
            exists = cursor.fetchone()

            if exists:
                log_message(f"Duplicate Dx_Code '{dx}' for {first_name} {last_name}")
                skipped += 1
                continue

            cursor.execute("""
                INSERT INTO Patient_Diagnoses (PatientGuid, Dx_Code)
                VALUES (?, ?)
            """, patient_guid, dx)
            inserted += 1

    conn.commit()
    cursor.close()
    conn.close()

    print(f"✅ Done. Inserted: {inserted}, Skipped: {skipped}, Invalid: {invalid}, Missing patients: {missing_patients}")
    log_message(f"SUMMARY — Inserted: {inserted}, Skipped: {skipped}, Invalid: {invalid}, Missing patients: {missing_patients}")

if __name__ == "__main__":
    load_diagnoses(r"D:\PatientData\Atrium Health Care Center_PatientData_03282025.csv")
