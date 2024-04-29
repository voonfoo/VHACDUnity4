cmake_minimum_required(VERSION 3.10)
project(UnityVHACD VERSION 1.0.0 LANGUAGES C CXX)

set(CMAKE_CXX_STANDARD 11)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)

set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -fvisibility=hidden -fvisibility-inlines-hidden")

set(HEADERS
        UnityVHACD.h
        VHACD.h
)

# Create the shared library
message(${PROJECT_NAME})
add_library(${PROJECT_NAME} SHARED UnityVHACD.cpp ${HEADERS})

install(TARGETS ${PROJECT_NAME}
        LIBRARY DESTINATION lib
        ARCHIVE DESTINATION lib
        RUNTIME DESTINATION bin
)