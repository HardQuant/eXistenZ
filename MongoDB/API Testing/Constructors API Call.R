library(jsonlite)

url <- "http://ergast.com/api/f1/2011/constructors.json"

response <- GET(url)

status_code(response)

if (status_code(response) == 200) {
  data <- content(response, "text")
  data_parsed <- fromJSON(data, flatten = FALSE)
  
  cat(toJSON(data_parsed, pretty = TRUE))
}else {
  print("request Failed")
}
#################################################################

library(httr)       # Handles HTTP requests
library(jsonlite)   # Parses JSON data

# Initialize variables
start_year <- 1986
end_year <- 2024
unique_fields <- c()  # Start with an empty list of unique fields

# Iterate through each year
for (year in start_year:end_year) {
  url <- paste0("http://ergast.com/api/f1/", year, "/constructors.json")
  print(paste("Fetching data for year:", year))
  
  response <- GET(url)
  
  if (status_code(response) == 200) {
    # Parse the JSON response
    data <- content(response, "text", encoding = "UTF-8")
    json_data <- fromJSON(data, flatten = FALSE)
    
    # Extract constructors data
    constructors <- json_data$MRData$ConstructorTable$Constructors
    
    # Check if constructors data is present
    if (is.null(constructors) || nrow(constructors) == 0) {
      print(paste("No constructors found for year:", year))
      next
    }
    
    # Extract column names (fields) from constructors
    new_fields <- colnames(constructors)  # Extract column names
    for (field in new_fields) {
      if (!(field %in% unique_fields)) {  # Add only new fields
        unique_fields <- c(unique_fields, field)
        print(paste("New field added:", field))
      }
    }
  } else {
    print(paste("Failed to fetch data for year:", year, "Status code:", status_code(response)))
  }
}

# Print the final list of unique fields
print("Final List of Unique Fields Across All Years:")
print(unique_fields)
