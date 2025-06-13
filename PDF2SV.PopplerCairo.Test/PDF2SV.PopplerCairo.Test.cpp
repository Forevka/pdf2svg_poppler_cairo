// PDF2SV.PopplerCairo.Test.cpp : This file contains the 'main' function. Program execution begins and ends there.
//

#include <iostream>
#include <fstream>
#include <vector>
#include <cassert>

// Import the C API
extern "C" {
    // must match the signature in your pdf2svglib.cpp
    __declspec(dllimport)
        unsigned char* pdf_page_to_svg_mem(
            const unsigned char* pdf_data,
            int                   pdf_len,
            int                   page_num,
            int* out_svg_len
        );

    // free() from the C runtime
    __declspec(dllimport)
        void free(void* ptr);
}

int main(int argc, char** argv) {
    if (argc != 4) {
        std::cerr << "Usage: " << argv[0] << " <input.pdf> <page-index> <output.svg>\n";
        return 1;
    }

    const char* pdfPath = argv[1];
    int         pageIndex = std::atoi(argv[2]);
    const char* svgPath = argv[3];

    std::ifstream in(pdfPath, std::ios::binary | std::ios::ate);
    if (!in) {
        std::cerr << "Failed to open PDF: " << pdfPath << "\n";
        return 2;
    }
    auto size = in.tellg();
    in.seekg(0, std::ios::beg);

    std::vector<unsigned char> pdfBuf(size);
    if (!in.read(reinterpret_cast<char*>(pdfBuf.data()), size)) {
        std::cerr << "Failed to read PDF data\n";
        return 3;
    }
    in.close();

    int svgLen = 0;
    unsigned char* svgBuf = pdf_page_to_svg_mem(
        pdfBuf.data(),
        static_cast<int>(pdfBuf.size()),
        pageIndex,
        &svgLen
    );
    if (!svgBuf || svgLen <= 0) {
        std::cerr << "pdf_to_svg_mem failed or returned empty result\n";
        return 4;
    }

    // --- 3. Write the SVG out to disk ---
    std::ofstream out(svgPath, std::ios::binary);
    assert(out && "Failed to open output SVG file");
    out.write(reinterpret_cast<char*>(svgBuf), svgLen);
    out.close();

    // --- 4. Clean up the native buffer ---
    free(svgBuf);

    std::cout << "Success! Wrote " << svgLen
        << " bytes of SVG to " << svgPath << "\n";
    return 0;
}

// Run program: Ctrl + F5 or Debug > Start Without Debugging menu
// Debug program: F5 or Debug > Start Debugging menu

// Tips for Getting Started: 
//   1. Use the Solution Explorer window to add/manage files
//   2. Use the Team Explorer window to connect to source control
//   3. Use the Output window to see build output and other messages
//   4. Use the Error List window to view errors
//   5. Go to Project > Add New Item to create new code files, or Project > Add Existing Item to add existing code files to the project
//   6. In the future, to open this project again, go to File > Open > Project and select the .sln file
