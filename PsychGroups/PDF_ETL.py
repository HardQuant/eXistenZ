import os
from pdf2image import convert_from_path
from pytesseract import image_to_string, pytesseract
from PIL import Image

# Explicitly set the path to Tesseract executable
pytesseract.tesseract_cmd = r'C:\Users\catpl\Documents\Tesseract\tesseract.exe'


def is_progress_note(text):
    # Prioritize progress note detection to minimize false positives
    keywords = [
        'GROUP THERAPY PROGRESS NOTE', 'Resident Name', 'Session Date', 'Length of Group', 
        'Purpose of Therapy', 'Themes Discussed', 'Mental Status', "Doctor's Signature", 'Progress made today',
        'This patient participated in group therapy facilitated', 'Progress Note', 'Brief Mental Status',
        'Topic:', 'Intervention:', 'Response:', 'Progress made today:', 'Progress made to date:'
    ]
    if any(keyword.lower() in text.lower() for keyword in keywords):
        return True
    return False


def is_sign_in_sheet(text):
    # Check for keywords specific to sign-in sheets, but only if progress note patterns are absent
    keywords = ['GROUP SIGN-IN', 'PARTICIPATED IN GROUP THERAPY', 'GROUP BILLING', 'START TIME', 'END TIME']
    list_pattern = ['1.', '2.', '3.', '4.', '5.', '6.', '7.', '8.', '9.', '10.', '11.', '12.']
    # Ensure that progress note indicators are not present
    if not is_progress_note(text):
        if any(keyword.lower() in text.lower() for keyword in keywords):
            return True
        if any(pattern in text for pattern in list_pattern):
            return True
        # Check for table-like structure common in sign-in sheets
        if 'Name' in text and 'Time' in text and 'Signature' in text:
            return True
    return False


def count_sign_in_and_progress_notes(pdf_path):
    try:
        # Convert all pages of the PDF to images, specifying the Poppler path explicitly
        all_images = convert_from_path(pdf_path, dpi=150, poppler_path=r"C:\Users\catpl\Documents\poppler-24.08.0\Library\bin")

        # Initialize counters
        sign_in_sheet_count = 0
        progress_note_count = 0

        # Loop through each image and determine its type
        total_pages = len(all_images)
        for page_number, image in enumerate(all_images, start=1):
            print(f"Processing page {page_number}/{total_pages}...")
            text = image_to_string(image)
            print(f"Extracted text from page {page_number}:\n{text}\n")
            # Check if the extracted text matches progress note first to reduce false positives
            if is_progress_note(text):
                progress_note_count += 1
                print(f"Identified as Progress Note (Page {page_number})")
            elif is_sign_in_sheet(text):
                sign_in_sheet_count += 1
                print(f"Identified as Sign-In Sheet (Page {page_number})")
            else:
                print(f"Unable to classify page {page_number}")
            print(f"Completed page {page_number}/{total_pages}")

        print(f"\nNumber of groups conducted (sign-in sheets): {sign_in_sheet_count}")
        print(f"Number of participants (progress notes): {progress_note_count}")

    except Exception as e:
        print(f"An error occurred: {str(e)}")


# Example usage
pdf_path = input("Enter the path to the PDF file: ")
count_sign_in_and_progress_notes(pdf_path)
