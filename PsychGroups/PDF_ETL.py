import os
import pyodbc
from datetime import datetime
from difflib import get_close_matches
import subprocess
from PyPDF2 import PdfReader

# Set up the connection to your SQL Server (Windows Authentication)
conn = pyodbc.connect(
    r'DRIVER={ODBC Driver 17 for SQL Server};'
    r'SERVER=DESKTOP-FIQB2SM\SQLEXPRESS;'
    r'DATABASE=PsychGroups;'
    r'Trusted_Connection=yes;'
)
cursor = conn.cursor()

# Facility dictionary
facility_dict = {
    'aperion': 'F2E26A74-4B81-4B48-9C61-2775E981EAFD',
    'chicago': 'D9FB5995-6EAE-4C67-A76A-2C15A5D46FAF',
    'atrium': '221A3BB8-76B1-4E83-9101-78D1C090D39F',
    'parkview': '099CAF67-6066-4452-A4B5-85D3951D91E7',
    'sheridan': 'ADBF0B59-8378-41B8-A3CD-AF4A61F919CF'
}

# Assistant dictionary
assistant_dict = {
    'ahni': 'C9C432E8-EBC5-4653-932B-35028FA04777',
    'ella': '0588B57E-8612-44CF-99F3-413421FB8B0B',
    'jordan': '031E97D9-BE5B-4E30-A83A-687FD58733F0',
    'samuel': '17479760-A5FC-45B3-9AAC-6B9B1C8443F4',
    'kaylie': 'BACF015D-940B-428A-A8F0-F31C244D12E2'
}

def open_pdf(file_path):
    try:
        if os.name == 'nt':
            os.startfile(file_path)
        else:
            subprocess.Popen(['open', file_path])
        print("PDF opened for confirmation.")
    except Exception as e:
        print(f"Error opening PDF: {e}")

def calculate_groups_and_participants(pdf_path):
    try:
        reader = PdfReader(pdf_path)
        nPages = len(reader.pages)
        if nPages <= 13:
            groups = 1
            participants = nPages - 1
        elif 13 < nPages <= 26:
            groups = 2
            participants = nPages - 2
        elif 26 < nPages <= 39:
            groups = 3
            participants = nPages - 3
        else:
            raise Exception("Number of pages exceeds the expected range for group classification.")
        return groups, participants
    except Exception as e:
        print(f"Error calculating groups and participants: {e}")
        return None, None

def get_doctor_guid(facility_guid):
    query = f"SELECT [DoctorGuid] FROM Facilities WHERE [FacilityGuid] = '{facility_guid}'"
    cursor.execute(query)
    result = cursor.fetchone()
    if result:
        return result[0]
    return None

def insert_into_practices(facility_guid, formatted_date, doctor_guid, assistant1_guid, assistant2_guid, groups, participants, forms):
    try:
        current_datetime = datetime.now().strftime("%Y-%m-%d %H:%M:%S.%f")[:-3]
        
        query = f"""
        INSERT INTO Practices ([FacilityGuid], [Date], [DoctorGuid], [Assistant1], [Assistant2], [Groups], [Participants], [Forms], [Created_DateTime], [Last_Updated])
        VALUES ('{facility_guid}', '{formatted_date}', '{doctor_guid}', 
        '{assistant1_guid}', {f"'{assistant2_guid}'" if assistant2_guid else 'NULL'}, 
        {groups}, {participants}, '{forms}', '{current_datetime}', '{current_datetime}')
        """
        cursor.execute(query)
        conn.commit()
        print("Record inserted successfully into Practices table.")
    except Exception as e:
        print(f"Error inserting into Practices table: {e}")

def get_assistant():
    print("Select an assistant by first name (or type 'none'):")
    while True:
        for name in assistant_dict.keys():
            print(name.capitalize())
        selected_name = input("Enter the assistant's first name: ").strip().lower()
        if selected_name == 'none':
            return None
        if selected_name in assistant_dict:
            return assistant_dict[selected_name]
        print("Invalid selection. Please try again.")

def check_duplicate(facility_guid, formatted_date):
    query = f"""
    SELECT * FROM Practices
    WHERE [FacilityGuid] = '{facility_guid}' AND [Date] = '{formatted_date}'
    """
    cursor.execute(query)
    if cursor.fetchone():
        raise Exception("Duplicate record found in the Practices table.")

def extract_facility_and_date(file_name):
    base_name = os.path.splitext(os.path.basename(file_name))[0]
    facility_name = ''.join([char for char in base_name if not char.isdigit()]).lower()
    matched = get_close_matches(facility_name, facility_dict.keys(), n=1, cutoff=0.6)
    if not matched:
        raise Exception(f"No facility match found for '{facility_name}'")
    facility_guid = facility_dict[matched[0]]
    formatted_date = datetime.strptime(base_name[-8:], "%m%d%Y").strftime("%Y-%m-%d")
    return facility_guid, formatted_date

# Main ETL process
try:
    # Prompt for the file path (Forms field) from user input
    file_path = input("Enter the PDF file path: ").strip().strip('"')
    forms = file_path  # Use the entered file path as the Forms field
    open_pdf(file_path)

    facility_guid, formatted_date = extract_facility_and_date(file_path)
    print(f"Facility GUID: {facility_guid}")
    print(f"Date: {formatted_date}")

    check_duplicate(facility_guid, formatted_date)
    print("No duplicates found. Proceeding with insertion.")

    groups, participants = calculate_groups_and_participants(file_path)
    print(f"Groups: {groups}, Participants: {participants}")

    print("Default Assistant1: Samuel")
    if input("Is this correct? (Y/N): ").strip().upper() != 'Y':
        assistant1_guid = get_assistant()
    else:
        assistant1_guid = assistant_dict['samuel']

    if input("Was there a second assistant? (Y/N): ").strip().upper() == 'Y':
        assistant2_guid = get_assistant()
    else:
        assistant2_guid = None

    doctor_guid = get_doctor_guid(facility_guid)
    print(f"Doctor GUID: {doctor_guid}")
    print(f"Assistant1 GUID: {assistant1_guid}")
    print(f"Assistant2 GUID: {assistant2_guid}")

    insert_into_practices(facility_guid, formatted_date, doctor_guid, assistant1_guid, assistant2_guid, groups, participants, forms)

except Exception as e:
    print(f"Error: {e}")
finally:
    cursor.close()
    conn.close()
