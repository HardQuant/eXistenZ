import os
import subprocess

def compress_with_ghostscript(input_path, output_path):
    try:
        # Full path to the Ghostscript executable
        gs_path = r"C:\Program Files\gs\gs10.05.0\bin\gswin64c.exe"
        
        # Command to call Ghostscript for PDF compression
        command = [
            gs_path, 
            "-sDEVICE=pdfwrite", 
            "-dCompatibilityLevel=1.4", 
            "-dPDFSETTINGS=/screen",  # Lower quality for smaller size
            "-dColorImageResolution=72",   # Lower image resolution for compression
            "-dGrayImageResolution=72",
            "-dMonoImageResolution=72",
            "-dNOPAUSE", 
            "-dQUIET", 
            "-dBATCH",
            f"-sOutputFile={output_path}", 
            input_path
        ]
        
        # Run the command and wait for completion
        result = subprocess.run(command, capture_output=True, text=True)

        # Check for errors during compression
        if result.returncode == 0:
            print(f"PDF successfully compressed and saved to: {output_path}")
        else:
            print(f"Error compressing PDF: {result.stderr}")
    except Exception as e:
        print(f"Exception occurred while compressing PDF: {e}")

def move_and_compress_pdf(file_path):
    # Set the destination folder
    compressed_dir = r"D:\PDFs\Sheridan\Data\Compressed PDFs"

    # Ensure the destination directory exists
    os.makedirs(compressed_dir, exist_ok=True)

    # Extract the file name from the original path
    file_name = os.path.basename(file_path)

    # Construct the destination path
    compressed_path = os.path.join(compressed_dir, file_name)

    # Compress and move the PDF file
    compress_with_ghostscript(file_path, compressed_path)

if __name__ == "__main__":
    # Prompt the user for the file path
    file_path = input("Enter the full path of the PDF file to compress: ")
    
    # Validate the file path
    if os.path.isfile(file_path) and file_path.lower().endswith('.pdf'):
        move_and_compress_pdf(file_path)
    else:
        print("Invalid file path. Please provide a valid PDF file.")
