import pyodbc
import pandas as pd

# File path to your cleaned CSV
file_path = r"C:\Users\catpl\OneDrive\Documents\VS Studio Projects\PsychGroups ETL\Data\Cleaned_Diagnoses_Data.csv"

# Load the CSV
df = pd.read_csv(file_path)

# Rename columns to match SQL table
df.columns = ["Dx_Code", "Disorder_Name"]

# Set up the connection to your SQL Server (Windows Authentication)
conn = pyodbc.connect(
    r'DRIVER={ODBC Driver 17 for SQL Server};'
    r'SERVER=DESKTOP-FIQB2SM\SQLEXPRESS;'
    r'DATABASE=PsychGroups;'
    r'Trusted_Connection=yes;'
)
cursor = conn.cursor()

# SQL INSERT command
insert_query = """
INSERT INTO Diagnoses (Dx_Code, Disorder_Name)
VALUES (?, ?)
"""

# Insert each row
for index, row in df.iterrows():
    cursor.execute(insert_query, row["Dx_Code"], row["Disorder_Name"])

# Commit and close
conn.commit()
cursor.close()
conn.close()

print("✅ Diagnoses successfully inserted into the database.")
