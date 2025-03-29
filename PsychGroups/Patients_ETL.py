import pandas as pd
import pyodbc
import uuid
from datetime import datetime
import re

# Shared DB connection string (reuse this across your ETL scripts)
DB_CONNECTION_STRING = (
    r'DRIVER={ODBC Driver 17 for SQL Server};'
    r'SERVER=DESKTOP-FIQB2SM\SQLEXPRESS;'
    r'DATABASE=PsychGroups;'
    r'Trusted_Connection=yes;'
)

def get_facility_guid(cursor, filename):
    match = re.match(r"(.*?)_PatientData_", filename)
    facility_name_fragment = match.group(1).replace("_", " ") if match else None

    if not facility_name_fragment:
        raise ValueError("Could not extract facility name from filename.")

    print(f"\n🔍 Looking for facility matching: '{facility_name_fragment}'")

    cursor.execute("SELECT FacilityGUID, Name FROM Facilities")
    facilities = cursor.fetchall()

    print("🏥 Available Facilities in DB:")
    for guid, name in facilities:
        print("-", name)

    # Match with space-insensitive and lowercase comparison
    fragment_clean = facility_name_fragment.lower().replace(" ", "")
    for guid, name in facilities:
        name_clean = name.lower().replace(" ", "")
        if fragment_clean in name_clean:
            print(f"\n✅ Match found: '{name}'\n")
            return guid, name

    raise ValueError(f"\n❌ Facility name like '{facility_name_fragment}' not found in Facilities table.\n")

def load_patient_data(csv_path):
    conn = pyodbc.connect(DB_CONNECTION_STRING)
    cursor = conn.cursor()

    filename = csv_path.split("\\")[-1]  # Windows path
    facility_guid, matched_facility_name = get_facility_guid(cursor, filename)

    df = pd.read_csv(csv_path)

    # Rename 'Foor' → 'Floor' if needed
    if 'Foor' in df.columns:
        df.rename(columns={'Foor': 'Floor'}, inplace=True)

    df['First_Name'] = df['First_Name'].astype(str).str.strip()
    df['Last_Name'] = df['Last_Name'].astype(str).str.strip()

    unique_patients = df.drop_duplicates(subset=['First_Name', 'Last_Name'])

    inserted_count = 0
    for _, row in unique_patients.iterrows():
        first_name = row['First_Name']
        last_name = row['Last_Name']

        # Safely cast Floor to int or None
        try:
            floor = int(row['Floor']) if pd.notnull(row['Floor']) else None
        except (ValueError, TypeError):
            print(f"⚠️ Bad floor value for {first_name} {last_name}: '{row['Floor']}' → setting to NULL")
            floor = None

        patient_guid = str(uuid.uuid4())
        now = datetime.now()

        cursor.execute("""
            SELECT 1 FROM Patient 
            WHERE First_Name = ? AND Last_Name = ? AND FacilityGUID = ?
        """, first_name, last_name, facility_guid)
        exists = cursor.fetchone()

        if not exists:
            cursor.execute("""
                INSERT INTO Patient (PatientGUID, First_Name, Last_Name, Floor, FacilityGUID, Created_DateTime, Last_Updated)
                VALUES (?, ?, ?, ?, ?, ?, ?)
            """, patient_guid, first_name, last_name, floor, facility_guid, now, now)
            inserted_count += 1

    print(f"\n🎉 Inserted {inserted_count} new patients for facility '{matched_facility_name}'.")

    # === Update Behavior_Desc if it's NULL or blank ===
    if 'Behavior_Desc' not in df.columns:
        print("⚠️ 'Behavior_Desc' column not found in CSV — skipping behavior updates.")
    else:
        print("\n🧠 Updating Behavior Descriptions...")
        update_count = 0

        for _, row in unique_patients.iterrows():
            first_name = row['First_Name']
            last_name = row['Last_Name']
            behavior_desc = row['Behavior_Desc']

            if pd.isnull(behavior_desc) or str(behavior_desc).strip() == "":
                continue

            cursor.execute("""
                SELECT Behavior_Desc FROM Patient
                WHERE First_Name = ? AND Last_Name = ? AND FacilityGUID = ?
            """, first_name, last_name, facility_guid)
            result = cursor.fetchone()

            if result and (result[0] is None or str(result[0]).strip() == ""):
                cursor.execute("""
                    UPDATE Patient
                    SET Behavior_Desc = ?
                    WHERE First_Name = ? AND Last_Name = ? AND FacilityGUID = ?
                """, behavior_desc.strip(), first_name, last_name, facility_guid)
                update_count += 1
                print(f"✅ Behavior_Desc updated for {first_name} {last_name}")

        print(f"\n🧾 {update_count} patient records updated with Behavior_Desc.")

    conn.commit()
    cursor.close()
    conn.close()

if __name__ == "__main__":
    load_patient_data(r"D:\PatientData\Atrium Health Care Center_PatientData_03282025.csv")
