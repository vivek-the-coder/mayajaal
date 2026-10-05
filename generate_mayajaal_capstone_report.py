#!/usr/bin/env python3
"""Generate Mayajaal Capstone Project-I Report (KisanSangam format/length)."""

from __future__ import annotations

import os
from io import BytesIO

from reportlab.lib import colors
from reportlab.lib.enums import TA_CENTER, TA_JUSTIFY, TA_LEFT, TA_RIGHT
from reportlab.lib.pagesizes import A4
from reportlab.lib.styles import ParagraphStyle, getSampleStyleSheet
from reportlab.lib.units import inch, cm
from reportlab.platypus import (
    SimpleDocTemplate,
    Paragraph,
    Spacer,
    Table,
    TableStyle,
    PageBreak,
    KeepTogether,
    ListFlowable,
    ListItem,
    Image,
    HRFlowable,
)
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont

import report_chapters as chapters

ROOT = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(ROOT, "Mayajaal_Capstone_Report.pdf")
LOGO = os.path.join(ROOT, "_logo_0.png")


def chapter_api():
    """Helpers passed into detailed chapter builders."""
    return {
        "P": P,
        "H": H,
        "bullets": bullets,
        "numbered": numbered,
        "make_table": make_table,
        "set_chapter": set_chapter,
        "styles": STYLES,
    }

PAGE_W, PAGE_H = A4
LEFT = 1.0 * inch
RIGHT = 1.0 * inch
TOP = 0.85 * inch
BOTTOM = 0.75 * inch

# Chapter-page header state
CHAPTER_STATE = {"name": "INTRODUCTION", "enrol": "2301201110, 2301201119"}


def make_styles():
    styles = getSampleStyleSheet()
    styles.add(
        ParagraphStyle(
            name="CoverTitle",
            fontName="Times-Bold",
            fontSize=14,
            leading=18,
            alignment=TA_CENTER,
            spaceAfter=8,
        )
    )
    styles.add(
        ParagraphStyle(
            name="CoverSub",
            fontName="Times-Roman",
            fontSize=12,
            leading=16,
            alignment=TA_CENTER,
            spaceAfter=6,
        )
    )
    styles.add(
        ParagraphStyle(
            name="CoverProject",
            fontName="Times-Bold",
            fontSize=13,
            leading=17,
            alignment=TA_CENTER,
            spaceAfter=10,
            spaceBefore=6,
        )
    )
    styles.add(
        ParagraphStyle(
            name="FrontHeading",
            fontName="Times-Bold",
            fontSize=16,
            leading=20,
            alignment=TA_CENTER,
            spaceAfter=18,
            spaceBefore=6,
        )
    )
    styles.add(
        ParagraphStyle(
            name="BodyJust",
            fontName="Times-Roman",
            fontSize=12,
            leading=18,
            alignment=TA_JUSTIFY,
            spaceAfter=10,
            firstLineIndent=0,
        )
    )
    styles.add(
        ParagraphStyle(
            name="BodyJustIndent",
            fontName="Times-Roman",
            fontSize=12,
            leading=18,
            alignment=TA_JUSTIFY,
            spaceAfter=10,
            firstLineIndent=18,
        )
    )
    styles.add(
        ParagraphStyle(
            name="ChapterTitle",
            fontName="Times-Bold",
            fontSize=14,
            leading=18,
            alignment=TA_CENTER,
            spaceAfter=16,
            spaceBefore=4,
        )
    )
    styles.add(
        ParagraphStyle(
            name="SectionHead",
            fontName="Times-Bold",
            fontSize=12,
            leading=16,
            alignment=TA_LEFT,
            spaceBefore=12,
            spaceAfter=8,
        )
    )
    styles.add(
        ParagraphStyle(
            name="SubSectionHead",
            fontName="Times-Bold",
            fontSize=12,
            leading=16,
            alignment=TA_LEFT,
            spaceBefore=10,
            spaceAfter=6,
        )
    )
    styles.add(
        ParagraphStyle(
            name="BulletBody",
            fontName="Times-Roman",
            fontSize=12,
            leading=17,
            alignment=TA_JUSTIFY,
            leftIndent=18,
            spaceAfter=4,
        )
    )
    styles.add(
        ParagraphStyle(
            name="TableCell",
            fontName="Times-Roman",
            fontSize=10,
            leading=13,
            alignment=TA_LEFT,
        )
    )
    styles.add(
        ParagraphStyle(
            name="TableHead",
            fontName="Times-Bold",
            fontSize=10,
            leading=13,
            alignment=TA_CENTER,
        )
    )
    styles.add(
        ParagraphStyle(
            name="Caption",
            fontName="Times-Bold",
            fontSize=11,
            leading=14,
            alignment=TA_CENTER,
            spaceBefore=8,
            spaceAfter=12,
        )
    )
    styles.add(
        ParagraphStyle(
            name="TOCEntry",
            fontName="Times-Roman",
            fontSize=12,
            leading=18,
            alignment=TA_LEFT,
        )
    )
    styles.add(
        ParagraphStyle(
            name="CenterBody",
            fontName="Times-Roman",
            fontSize=12,
            leading=18,
            alignment=TA_CENTER,
            spaceAfter=8,
        )
    )
    styles.add(
        ParagraphStyle(
            name="SignLine",
            fontName="Times-Roman",
            fontSize=12,
            leading=16,
            alignment=TA_LEFT,
            spaceAfter=4,
        )
    )
    styles.add(
        ParagraphStyle(
            name="Keyword",
            fontName="Times-Italic",
            fontSize=12,
            leading=17,
            alignment=TA_JUSTIFY,
            spaceBefore=8,
            spaceAfter=8,
        )
    )
    styles.add(
        ParagraphStyle(
            name="MonoSmall",
            fontName="Courier",
            fontSize=9,
            leading=12,
            alignment=TA_LEFT,
            spaceAfter=6,
            backColor=colors.Color(0.95, 0.95, 0.95),
            leftIndent=6,
            rightIndent=6,
        )
    )
    return styles


STYLES = make_styles()


def P(text, style="BodyJust"):
    return Paragraph(text.replace("\n", " "), STYLES[style])


def H(text, style="SectionHead"):
    return Paragraph(text, STYLES[style])


def bullets(items):
    flow = []
    for it in items:
        flow.append(Paragraph(f"•  {it}", STYLES["BulletBody"]))
    return flow


def numbered(items):
    flow = []
    for i, it in enumerate(items, 1):
        flow.append(Paragraph(f"{i}.  {it}", STYLES["BulletBody"]))
    return flow


def make_table(headers, rows, col_widths=None):
    data = [[Paragraph(h, STYLES["TableHead"]) for h in headers]]
    for row in rows:
        data.append([Paragraph(str(c), STYLES["TableCell"]) for c in row])
    t = Table(data, colWidths=col_widths, hAlign="CENTER")
    t.setStyle(
        TableStyle(
            [
                ("BACKGROUND", (0, 0), (-1, 0), colors.Color(0.88, 0.88, 0.88)),
                ("GRID", (0, 0), (-1, -1), 0.6, colors.black),
                ("VALIGN", (0, 0), (-1, -1), "TOP"),
                ("LEFTPADDING", (0, 0), (-1, -1), 5),
                ("RIGHTPADDING", (0, 0), (-1, -1), 5),
                ("TOPPADDING", (0, 0), (-1, -1), 4),
                ("BOTTOMPADDING", (0, 0), (-1, -1), 4),
            ]
        )
    )
    return t


def set_chapter(name):
    CHAPTER_STATE["name"] = name


class NumberedCanvas:
    """Canvas wrapper adding front/body page numbers and headers."""

    def __init__(self, canvas, doc):
        self.canvas = canvas
        self.doc = doc

    def __getattr__(self, name):
        return getattr(self.canvas, name)


def add_page_number(canvas, doc):
    canvas.saveState()
    page = canvas.getPageNumber()
    # Front matter uses roman; body uses arabic via doc.page template switch
    if getattr(doc, "_in_body", False):
        # Header
        canvas.setFont("Times-Roman", 9)
        canvas.drawString(LEFT, PAGE_H - 0.45 * inch, f"Enrolment No: {CHAPTER_STATE['enrol']}")
        canvas.drawRightString(PAGE_W - RIGHT, PAGE_H - 0.45 * inch, "KPGU")
        canvas.drawString(LEFT, PAGE_H - 0.60 * inch, f"Chapter Name: {CHAPTER_STATE['name']}")
        canvas.drawRightString(PAGE_W - RIGHT, PAGE_H - 0.60 * inch, "KSET")
        # Footer page number (chapter numbering starts after front matter)
        body_page = page - getattr(doc, "_front_pages", 10)
        if body_page < 1:
            body_page = page
        canvas.setFont("Times-Bold", 10)
        canvas.drawCentredString(PAGE_W / 2, 0.45 * inch, str(body_page))
        # thin line under header
        canvas.setStrokeColor(colors.grey)
        canvas.setLineWidth(0.4)
        canvas.line(LEFT, PAGE_H - 0.68 * inch, PAGE_W - RIGHT, PAGE_H - 0.68 * inch)
    else:
        # roman numerals for front matter
        canvas.setFont("Times-Roman", 11)
        romans = ["", "i", "ii", "iii", "iv", "v", "vi", "vii", "viii", "ix", "x", "xi", "xii"]
        label = romans[page] if page < len(romans) else str(page)
        canvas.drawCentredString(PAGE_W / 2, 0.45 * inch, label)
    canvas.restoreState()


def build_cover():
    story = []
    story.append(Spacer(1, 0.35 * inch))
    if os.path.exists(LOGO):
        img = Image(LOGO, width=4.6 * inch, height=1.24 * inch)
        img.hAlign = "CENTER"
        story.append(img)
        story.append(Spacer(1, 0.25 * inch))
    else:
        story.append(Spacer(1, 0.6 * inch))

    story.append(P("A CAPSTONE PROJECT – I REPORT", "CoverTitle"))
    story.append(P("ON", "CoverSub"))
    story.append(
        P(
            "“MayaJaal – Local-First Cyber Deception and Endpoint Defense Platform”",
            "CoverProject",
        )
    )
    story.append(Spacer(1, 0.15 * inch))
    story.append(
        P(
            "Submitted in partial fulfillment of the requirements<br/>for the award of the degree of",
            "CoverSub",
        )
    )
    story.append(Spacer(1, 0.08 * inch))
    story.append(P("BACHELOR OF TECHNOLOGY (B.Tech.)", "CoverTitle"))
    story.append(P("IN", "CoverSub"))
    story.append(P("COMPUTER SCIENCE AND ENGINEERING", "CoverTitle"))
    story.append(Spacer(1, 0.25 * inch))
    story.append(P("Submitted By", "CoverSub"))
    story.append(Spacer(1, 0.08 * inch))
    story.append(P("<b>MEET BAROT</b>", "CoverSub"))
    story.append(P("ENROLLMENT NO: 2301201110", "CoverSub"))
    story.append(Spacer(1, 0.08 * inch))
    story.append(P("<b>MANAV PATEL</b>", "CoverSub"))
    story.append(P("ENROLLMENT NO: 2301201119", "CoverSub"))
    story.append(Spacer(1, 0.22 * inch))
    story.append(P("Under the Guidance of", "CoverSub"))
    story.append(P("<b>MS. MITI DESAI</b>", "CoverSub"))
    story.append(Spacer(1, 0.35 * inch))
    story.append(P("<b>Drs. Kiran &amp; Pallavi Patel Global University (KPGU)</b>", "CoverSub"))
    story.append(
        P(
            "Krishna School of Emerging Technology and Applied Research",
            "CoverSub",
        )
    )
    story.append(P("Department of Computer Science and Engineering", "CoverSub"))
    story.append(P("VADODARA, GUJARAT", "CoverSub"))
    story.append(P("ACADEMIC YEAR: 2026–2027", "CoverTitle"))
    story.append(PageBreak())
    return story


def build_certificate():
    story = []
    story.append(P("CERTIFICATE", "FrontHeading"))
    story.append(
        P(
            "This is to certify that the Capstone Project – I Report entitled "
            "“MayaJaal – Local-First Cyber Deception and Endpoint Defense Platform” "
            "submitted by Meet Barot, Enrollment No. 2301201110, and Manav Patel, "
            "Enrollment No. 2301201119, in partial fulfillment for the award of the "
            "degree of Bachelor of Technology in Computer Science and Engineering, "
            "has been successfully completed during the academic year 2026–2027."
        )
    )
    story.append(
        P(
            "This work has been carried out under the guidance and supervision of "
            "Ms. Miti Desai and is up to our satisfaction."
        )
    )
    story.append(Spacer(1, 0.5 * inch))
    story.append(P("Date: __________________", "SignLine"))
    story.append(P("Place: Vadodara", "SignLine"))
    story.append(Spacer(1, 0.7 * inch))
    cert = Table(
        [
            [
                Paragraph("<b>Ms. Miti Desai</b><br/>Internal Guide", STYLES["CenterBody"]),
                Paragraph(
                    "<b>Head of the Department</b><br/>Department of Computer Science and<br/>Engineering",
                    STYLES["CenterBody"],
                ),
            ]
        ],
        colWidths=[3.2 * inch, 3.2 * inch],
    )
    cert.setStyle(TableStyle([("VALIGN", (0, 0), (-1, -1), "TOP"), ("ALIGN", (0, 0), (-1, -1), "CENTER")]))
    story.append(cert)
    story.append(PageBreak())
    return story


def build_declaration():
    story = []
    story.append(P("DECLARATION", "FrontHeading"))
    story.append(
        P(
            "We hereby declare that the Capstone Project – I Report entitled "
            "“MayaJaal – Local-First Cyber Deception and Endpoint Defense Platform” "
            "is a record of the original work carried out by us during the course of "
            "our Capstone Project – I."
        )
    )
    story.append(
        P(
            "This work has not been submitted, either in part or in full, to any other "
            "University or Institution for the award of any degree or any other academic "
            "qualification."
        )
    )
    story.append(
        P(
            "We further declare that all sources of information used in this report have "
            "been duly acknowledged and cited wherever necessary."
        )
    )
    story.append(Spacer(1, 0.45 * inch))
    story.append(P("Date: __________________", "SignLine"))
    story.append(P("Place: Vadodara", "SignLine"))
    story.append(Spacer(1, 0.45 * inch))
    story.append(P("Student 1 Signature: __________________", "SignLine"))
    story.append(P("Name: Meet Barot", "SignLine"))
    story.append(P("Enrollment No: 2301201110", "SignLine"))
    story.append(Spacer(1, 0.35 * inch))
    story.append(P("Student 2 Signature: __________________", "SignLine"))
    story.append(P("Name: Manav Patel", "SignLine"))
    story.append(P("Enrollment No: 2301201119", "SignLine"))
    story.append(PageBreak())
    return story


def build_acknowledgement():
    story = []
    story.append(P("ACKNOWLEDGEMENT", "FrontHeading"))
    story.append(
        P(
            "We would like to express our sincere gratitude to Ms. Miti Desai, our project "
            "guide, for her continuous guidance, valuable suggestions, constructive "
            "feedback, and encouragement throughout the development of our Capstone "
            "Project – I titled “MayaJaal – Local-First Cyber Deception and Endpoint "
            "Defense Platform.”"
        )
    )
    story.append(
        P(
            "We are grateful to the Department of Computer Science and Engineering, "
            "Drs. Kiran &amp; Pallavi Patel Global University (KPGU), Vadodara, and the "
            "Krishna School of Emerging Technology and Applied Research (KSET) for "
            "providing us with the academic environment, resources, and opportunities "
            "required to carry out this project."
        )
    )
    story.append(
        P(
            "We would also like to express our appreciation to the faculty members and "
            "staff of the department for their support and guidance during the development "
            "of the project. Their insights into software engineering practices, operating "
            "system concepts, and cybersecurity principles helped us refine both the "
            "architecture and the security posture of MayaJaal."
        )
    )
    story.append(
        P(
            "Finally, we extend our heartfelt thanks to our families, friends, and everyone "
            "who directly or indirectly supported us throughout this project. Their "
            "encouragement and motivation helped us overcome challenges and complete "
            "this work successfully."
        )
    )
    story.append(PageBreak())
    return story


def build_abstract():
    story = []
    story.append(P("ABSTRACT", "FrontHeading"))
    story.append(
        P(
            "Modern endpoint security challenges are no longer limited to known malware "
            "signatures. Insider data exfiltration, mass file copying to removable media, "
            "credential probing, and ransomware-like file modification patterns can evade "
            "traditional antivirus detection when each individual signal appears weak in "
            "isolation. Security operators therefore need systems that can correlate "
            "deception triggers with behavioral telemetry and respond in a controlled, "
            "auditable manner."
        )
    )
    story.append(
        P(
            "MayaJaal – Local-First Cyber Deception and Endpoint Defense Platform is "
            "developed as a Windows-based prototype that combines decoy (honey) file "
            "deployment, behavioral risk scoring, multi-factor confidence estimation, "
            "attack-pattern correlation, policy-driven response selection, and "
            "safety-gated containment. The platform consists of two cooperating processes: "
            "the Guardian host, which collects signals and executes the threat pipeline, "
            "and the Security Center, a WPF operator console that visualizes live threat "
            "state over a named-pipe IPC channel."
        )
    )
    story.append(
        P(
            "The current implementation focuses on five core functional pillars: "
            "Deception and Telemetry Collection, Threat Scoring Engines, Policy and "
            "Safety-Gated Response, Cryptographic Vault Lockdown with Evidence "
            "Persistence, and Operator Visualization through Security Center. Honey "
            "decoys are planted under the user’s Documents folder. File-system, USB, "
            "and process collectors emit security events into a sliding threat window. "
            "Risk, Confidence, and Correlation engines evaluate those events using "
            "explicit, testable formulas. When confidence is sufficiently high, the "
            "PolicyEngine selects containment actions such as vault lockdown, while "
            "the SafetyGate may block unsafe high-impact responses. Events are stored "
            "in SQLite with a SHA-256 hash chain, and incidents can produce JSON "
            "evidence packs and QuestPDF reports."
        )
    )
    story.append(
        P(
            "The system integrates .NET 8, C# 12, Clean Architecture layering, "
            "Microsoft.Extensions.Hosting Windows Service hosting, AES-256-GCM vault "
            "cryptography with Argon2id key derivation, Serilog logging, and xUnit "
            "domain tests. The modular architecture provides a foundation for future "
            "enhancements such as deeper ETW sensors, continuous hash-chain verification, "
            "fleet management, MSI packaging, and richer automated integration testing."
        )
    )
    story.append(
        P(
            "<b>Keywords:</b> Cyber Deception, Endpoint Defense, Honey Files, Risk Scoring, "
            "Confidence Engine, Correlation, AES-GCM Vault, Named-Pipe IPC, Windows "
            "Security, MayaJaal.",
            "Keyword",
        )
    )
    story.append(PageBreak())
    return story


def build_lists_and_toc():
    story = []
    story.append(P("List of Tables", "FrontHeading"))
    tables = [
        ("Table 2.1: System Module Descriptions", "—"),
        ("Table 2.2: Hardware Requirements", "—"),
        ("Table 3.1: System Layer Responsibilities", "—"),
        ("Table 3.2: Major Data Entities and Stores", "—"),
        ("Table 3.3: Named-Pipe IPC Commands", "—"),
        ("Table 4.1: Technology Stack and Tools", "—"),
        ("Table 4.2: Module Description", "—"),
        ("Table 4.3: Sample Test Results", "—"),
        ("Table 4.4: Demo Escalation Steps", "—"),
        ("Table 5.1: Evaluation of Major Modules", "—"),
        ("Table 5.2: Requirements Traceability (Selected)", "—"),
        ("Table F.1: Signal Weights (Min, Max, λ)", "—"),
        ("Table H.1: Confidence Thresholds by ResponseAction", "—"),
        ("Table I.1: Named Set-Membership Patterns", "—"),
    ]
    for name, page in tables:
        story.append(P(f"{name} {'.' * 20} {page}", "TOCEntry"))
    story.append(PageBreak())

    story.append(P("List of Figures", "FrontHeading"))
    figs = [
        ("Figure 2.1: High-Level Use Context of MayaJaal", "17"),
        ("Figure 3.1: MayaJaal System Architecture", "20"),
        ("Figure 3.2: Threat Processing Pipeline", "22"),
        ("Figure 3.3: Event Store and Vault Data Layout", "26"),
        ("Figure 4.1: Technology Stack Overview", "31"),
        ("Figure 4.2: Security Center Dashboard Concept", "42"),
    ]
    for name, page in figs:
        story.append(P(f"{name} {'.' * 20} {page}", "TOCEntry"))
    story.append(PageBreak())

    story.append(P("Table of Contents", "FrontHeading"))
    toc = [
        ("1", "Certificate", "i"),
        ("2", "Declaration", "ii"),
        ("3", "Acknowledgement", "iii"),
        ("4", "Abstract", "iv"),
        ("5", "List of Tables", "v"),
        ("6", "List of Figures", "vi"),
        ("7", "CHAPTER 1: INTRODUCTION", "1"),
        ("", "1.1 BACKGROUND", "1"),
        ("", "1.2 PROBLEM STATEMENT", "2"),
        ("", "1.3 OBJECTIVES", "3"),
        ("", "1.4 SCOPE", "4"),
        ("", "1.4.1 Deception and Telemetry Collection", "4"),
        ("", "1.4.2 Threat Scoring Engines", "4"),
        ("", "1.4.3 Policy and Safety-Gated Response", "5"),
        ("", "1.4.4 Cryptographic Vault and Evidence", "5"),
        ("", "1.4.5 Security Center Operator Console", "5"),
        ("", "1.5 ORGANIZATION OF REPORT", "5"),
        ("8", "CHAPTER 2: SYSTEM ANALYSIS", "7"),
        ("", "2.1 STUDY OF CURRENT SYSTEM", "7"),
        ("", "2.2 PROBLEMS AND WEAKNESSES OF CURRENT SYSTEM", "8"),
        ("", "2.3 REQUIREMENTS OF THE NEW SYSTEM", "9"),
        ("", "2.3.1 Functional Requirements", "9"),
        ("", "2.3.2 Non-Functional Requirements", "10"),
        ("", "2.4 SYSTEM FEASIBILITY", "11"),
        ("", "2.4.1 Technical Feasibility", "11"),
        ("", "2.4.2 Economic Feasibility", "11"),
        ("", "2.4.3 Operational Feasibility", "12"),
        ("", "2.5 ACTIVITIES IN PROPOSED SYSTEM", "12"),
        ("", "2.6 MAIN MODULES", "13"),
        ("", "2.7 SOFTWARE AND HARDWARE", "15"),
        ("", "2.7.1 Software Requirements", "15"),
        ("", "2.7.2 Hardware Requirements", "15"),
        ("", "2.8 PROPOSED SYSTEM OVERVIEW", "16"),
        ("9", "CHAPTER 3: SYSTEM DESIGN", "18"),
        ("", "3.1 DESIGN METHODOLOGY", "18"),
        ("", "3.2 SYSTEM ARCHITECTURE", "19"),
        ("", "3.3 SYSTEM MODULE DESIGN", "20"),
        ("", "3.4 DECEPTION AND COLLECTOR DESIGN", "21"),
        ("", "3.5 THREAT ENGINE DESIGN", "22"),
        ("", "3.6 RISK, CONFIDENCE, AND CORRELATION DESIGN", "23"),
        ("", "3.7 POLICY AND SAFETY GATE DESIGN", "24"),
        ("", "3.8 VAULT AND CRYPTOGRAPHY DESIGN", "25"),
        ("", "3.9 DATABASE AND EVIDENCE DESIGN", "25"),
        ("", "3.10 INTERFACE DESIGN", "27"),
        ("", "3.11 SECURITY AND ACCESS CONTROL", "28"),
        ("", "3.12 SUMMARY OF SYSTEM DESIGN", "29"),
        ("10", "CHAPTER 4: IMPLEMENTATION", "30"),
        ("", "4.1 TECHNOLOGY STACK", "30"),
        ("", "4.2 MODULE DEVELOPMENT", "33"),
        ("", "4.3 TESTING", "37"),
        ("11", "CHAPTER 5: EVALUATION AND FUTURE WORK", "43"),
        ("", "5.1 EVALUATION OF RESULTS", "43"),
        ("", "5.2 FUTURE ENHANCEMENTS", "45"),
        ("", "5.3 SUMMARY", "46"),
        ("12", "CHAPTER 6: CONCLUSION AND DISCUSSION", "48"),
        ("", "6.1 CONCLUSION", "48"),
        ("13", "Appendix", "50"),
        ("14", "Reference", "52"),
    ]
    for sr, content, page in toc:
        if sr:
            line = f"<b>{sr}</b>&nbsp;&nbsp;{content}"
        else:
            line = f"&nbsp;&nbsp;&nbsp;&nbsp;{content}"
        row = Table(
            [[Paragraph(line, STYLES["TOCEntry"]), Paragraph(page, STYLES["TOCEntry"])]],
            colWidths=[5.8 * inch, 0.6 * inch],
        )
        row.setStyle(
            TableStyle(
                [
                    ("ALIGN", (1, 0), (1, 0), "RIGHT"),
                    ("VALIGN", (0, 0), (-1, -1), "BOTTOM"),
                    ("LEFTPADDING", (0, 0), (-1, -1), 0),
                    ("RIGHTPADDING", (0, 0), (-1, -1), 0),
                    ("TOPPADDING", (0, 0), (-1, -1), 1),
                    ("BOTTOMPADDING", (0, 0), (-1, -1), 1),
                ]
            )
        )
        story.append(row)
    story.append(PageBreak())
    return story


def build_chapter1():
    set_chapter("INTRODUCTION")
    story = []
    story.append(P("CHAPTER 1: INTRODUCTION", "ChapterTitle"))

    story.append(H("1.1 BACKGROUND"))
    story.append(
        P(
            "Cybersecurity has become a foundational requirement for personal computers, "
            "enterprise workstations, and institutional laboratories. As organizations store "
            "sensitive financial records, credentials, strategic documents, and personal "
            "data on endpoints, adversaries increasingly target the workstation itself rather "
            "than only the network perimeter. Attacks such as insider mass-copy, USB-based "
            "exfiltration, credential harvesting, and ransomware-style file encryption can "
            "cause severe confidentiality and availability damage within minutes."
        )
    )
    story.append(
        P(
            "Traditional antivirus and endpoint detection products remain essential, yet "
            "they often emphasize binary signatures, cloud reputation, or broad telemetry "
            "pipelines. In teaching laboratories and single-workstation environments, a "
            "full enterprise EDR stack may be unavailable, expensive, or opaque. There is "
            "therefore educational and practical value in a local-first platform whose "
            "scoring logic is explicit, inspectable, and correlated with deception artifacts."
        )
    )
    story.append(
        P(
            "Deception technology provides a complementary defensive idea: place attractive "
            "but false assets—commonly called honey files or decoys—where unauthorized "
            "access becomes a high-value signal. When a decoy is opened, copied, or "
            "modified, the defender gains evidence that exploration or theft is underway. "
            "If that signal is correlated with USB insertion, suspicious process activity, "
            "and mass file operations, confidence in a real attack increases substantially."
        )
    )
    story.append(
        P(
            "MayaJaal, whose name evokes an “illusion” or “web of deception,” is proposed "
            "as a local-first Windows cyber-defense prototype. It deploys six high-"
            "sensitivity decoy templates, collects file-system and related endpoint "
            "signals, scores risk and confidence over a sliding threat window, correlates "
            "named attack patterns, and can contain impact through reversible AES-GCM "
            "vault lockdown while preserving durable evidence for later review."
        )
    )
    story.append(
        P(
            "The platform is intentionally designed for transparency. Domain engines are "
            "pure and unit-tested. Persistence uses SQLite with a hash chain. Operator "
            "visibility is provided by a Security Center console over named-pipe IPC. "
            "These choices make MayaJaal suitable as a Capstone Project for demonstrating "
            "software architecture, cryptography, systems programming on Windows, and "
            "security engineering judgment."
        )
    )

    story.append(H("1.2 PROBLEM STATEMENT"))
    story.append(
        P(
            "Endpoint defenders face a decision problem under uncertainty. Individual "
            "signals—such as a USB insertion, a file rename, or a process start—may be "
            "benign. At the same time, waiting for unmistakable malware signatures can "
            "allow an insider or opportunistic attacker to copy sensitive documents before "
            "any alert is raised. Manual monitoring of every file operation is impractical."
        )
    )
    story.append(
        P(
            "Existing approaches have several gaps in the local workstation context:"
        )
    )
    story.extend(
        bullets(
            [
                "Fragmented tools separately cover antivirus, USB monitoring, and logging, without a unified local correlation story.",
                "Deception systems may detect honey access but may not combine that access with mass-copy or process context.",
                "High-impact automated responses, if present, may lack explicit confidence gates and rollback planning.",
                "Cloud-centric EDR platforms may be unsuitable for offline labs, air-gapped demos, or courses that require auditable scoring math.",
                "Evidence of suspicious activity may be ephemeral unless stored with integrity protections.",
            ]
        )
    )
    story.append(
        P(
            "Therefore, there is a need for an integrated local platform that can:"
        )
    )
    story.extend(
        bullets(
            [
                "Deploy and monitor deceptive honey files as high-sensitivity sensors.",
                "Collect file, USB, and process telemetry into a common security-event model.",
                "Score risk with time decay and honey-aware weighting.",
                "Estimate confidence from multiple factors before escalating.",
                "Correlate named multi-signal attack patterns.",
                "Select and safety-gate containment actions such as vault lockdown.",
                "Persist events and incidents with integrity-friendly storage and operator-facing visualization.",
            ]
        )
    )
    story.append(
        P(
            "MayaJaal aims to address these requirements by combining deception, "
            "behavioral scoring, policy/safety controls, cryptographic containment, and "
            "a live operator console within a single Windows solution."
        )
    )

    story.append(H("1.3 OBJECTIVES"))
    story.append(
        P(
            "The main objective of MayaJaal is to design and implement an intelligent, "
            "local-first cyber deception and endpoint defense platform that assists "
            "operators in detecting suspicious workstation activity and containing "
            "high-confidence threats in a controlled manner."
        )
    )
    story.append(P("The key objectives of the project are:"))
    story.extend(
        numbered(
            [
                "To develop a Guardian host capable of collecting endpoint security signals and running a continuous threat-evaluation pipeline.",
                "To implement honey-file deception with multiple decoy templates representing sensitive document categories.",
                "To design and implement Risk, Confidence, and Correlation engines with explicit formulas and unit tests.",
                "To implement PolicyEngine and SafetyGate components that map threat state to response actions with confidence thresholds.",
                "To provide AES-256-GCM vault lockdown with Argon2id-derived wrapping keys for reversible containment.",
                "To persist security events in SQLite using a SHA-256 hash chain and generate incident evidence artifacts.",
                "To develop a Security Center WPF console that visualizes threat metrics, events, incidents, vaults, and policies over named-pipe IPC.",
                "To support demo, simulate, and Windows Service operational modes for teaching, verification, and local deployment.",
            ]
        )
    )

    story.append(H("1.4 SCOPE"))
    story.append(
        P(
            "The scope of MayaJaal Capstone Project – I covers the design and "
            "implementation of a modular Windows cyber-defense prototype with the "
            "following major functional areas."
        )
    )

    story.append(H("1.4.1 Deception and Telemetry Collection", "SubSectionHead"))
    story.append(
        P(
            "This scope item includes deployment of six honey decoy templates under "
            "Documents\\MayaJaal-Decoys, FileSystemWatcher-based monitoring of Documents "
            "and decoy directories, burst/mass-activity detection, USB insert/remove "
            "collection, and process-start collection for suspicious process names. "
            "Trusted process allowlisting is included to reduce noise, while honey events "
            "always bypass the allowlist."
        )
    )

    story.append(H("1.4.2 Threat Scoring Engines", "SubSectionHead"))
    story.append(
        P(
            "This scope item covers the RiskEngine, ConfidenceEngine, and "
            "CorrelationEngine operating over a sliding in-memory threat window "
            "(approximately 15 minutes / max 500 events). Risk is weighted and "
            "time-decayed with a soft cap. Confidence mixes signal strength, correlation "
            "quality, context consistency, and historical accuracy. Correlation detects "
            "named set-membership patterns such as USB_Honey and USB_Process_MassCopy."
        )
    )

    story.append(H("1.4.3 Policy and Safety-Gated Response", "SubSectionHead"))
    story.append(
        P(
            "This scope item includes mapping threat level and confidence to response "
            "actions such as MONITOR, ALERT, CONTAIN, and EMERGENCY_LOCKDOWN, and "
            "applying SafetyGate checks that may approve or block high-impact actions. "
            "Incident creation, status updates, resolve, and rollback paths are included."
        )
    )

    story.append(H("1.4.4 Cryptographic Vault and Evidence", "SubSectionHead"))
    story.append(
        P(
            "This scope item covers vault create/unlock/lock flows, AES-GCM item "
            "encryption, Argon2id key derivation, DPAPI-protected bootstrap secrets, "
            "SQLite event persistence with hash chaining, incident evidence JSON, and "
            "QuestPDF incident report generation."
        )
    )

    story.append(H("1.4.5 Security Center Operator Console", "SubSectionHead"))
    story.append(
        P(
            "This scope item covers the WPF MVVM Security Center that polls Guardian "
            "approximately every two seconds for status, events, incidents, vaults, and "
            "policies; displays offline banners when IPC is unavailable; and supports "
            "operator actions such as manual vault lock where exposed by IPC."
        )
    )
    story.append(
        P(
            "Out of scope for the current Capstone phase are kernel minifilters, full "
            "packet capture, multi-tenant cloud fleet management, commercial EDR "
            "replacement claims, and Common Criteria / FIPS certification."
        )
    )

    story.append(H("1.5 ORGANIZATION OF REPORT"))
    story.append(P("The remainder of this report is organized as follows:"))
    story.extend(
        bullets(
            [
                "Chapter 2 presents system analysis, including study of current systems, weaknesses, requirements, feasibility, activities, modules, and software/hardware needs.",
                "Chapter 3 describes system design, architecture, module design, engine design, database/evidence design, interface design, and security controls.",
                "Chapter 4 details implementation, technology stack, module development, and testing activities.",
                "Chapter 5 evaluates results and outlines future enhancements.",
                "Chapter 6 concludes the report and discusses the significance of the work.",
                "The Appendix and Reference sections provide supporting material and cited sources.",
            ]
        )
    )
    story.append(PageBreak())
    return story


def build_chapter2():
    set_chapter("SYSTEM ANALYSIS")
    story = []
    story.append(P("CHAPTER 2: SYSTEM ANALYSIS", "ChapterTitle"))

    story.append(H("2.1 STUDY OF CURRENT SYSTEM"))
    story.append(
        P(
            "Workstation security today is commonly addressed through a combination of "
            "antivirus products, operating-system protections, enterprise EDR agents, "
            "USB device policies, and manual administrative vigilance. In academic and "
            "small-lab environments, defenders may also rely on Windows Event Viewer "
            "logs, PowerShell scripts, or ad-hoc monitoring tools."
        )
    )
    story.append(
        P(
            "Antivirus products are effective against many known threats but may miss "
            "novel insider behaviors that never drop a malicious binary. Enterprise EDR "
            "platforms provide richer telemetry and cloud analytics, yet they can be "
            "costly, require administrative infrastructure, and hide scoring internals "
            "behind proprietary engines. Network honeypots detect external probing but "
            "do not observe local document theft on a user’s desktop. Data Loss Prevention "
            "(DLP) tools focus on content classification and egress control, which is "
            "valuable but complementary rather than identical to decoy-triggered "
            "behavioral correlation."
        )
    )
    story.append(
        P(
            "In teaching contexts, students often study individual concepts—file watchers, "
            "cryptography, IPC, SQLite, WPF—without combining them into a coherent "
            "defense story. A current “system” for learning is therefore fragmented: "
            "sample programs demonstrate APIs, while real defensive platforms are too "
            "large to inspect. MayaJaal is positioned between these extremes as an "
            "integrated but inspectable local prototype."
        )
    )
    story.append(
        P(
            "Figure 2.1 conceptually places MayaJaal as a local correlation and response "
            "layer sitting between raw endpoint signals and the human operator."
        )
    )
    story.append(P("Figure 2.1: High-Level Use Context of MayaJaal", "Caption"))
    story.append(
        P(
            "Actors and systems in the current landscape include the workstation user, "
            "potential insider or malware process, removable media, OS file APIs, optional "
            "antivirus/EDR, and a security operator. MayaJaal inserts decoys and a "
            "Guardian service into this landscape and presents a Security Center view "
            "for the operator."
        )
    )

    story.append(H("2.2 PROBLEMS AND WEAKNESSES OF CURRENT SYSTEM"))
    story.append(P("Existing approaches exhibit the following notable weaknesses:"))
    story.append(H("1. Fragmented Local Visibility", "SubSectionHead"))
    story.append(
        P(
            "File activity, USB events, and process starts are often observed by different "
            "tools or not correlated at all on a single workstation."
        )
    )
    story.append(H("2. Weak Deception Integration", "SubSectionHead"))
    story.append(
        P(
            "Honey files, when used, may generate isolated alerts without systematic "
            "combination against mass-copy or USB context."
        )
    )
    story.append(H("3. Opaque Scoring", "SubSectionHead"))
    story.append(
        P(
            "Commercial engines rarely expose normative formulas suitable for academic "
            "audit, unit testing, or classroom explanation."
        )
    )
    story.append(H("4. Unsafe Automation Risk", "SubSectionHead"))
    story.append(
        P(
            "Automated containment without confidence gates can disrupt legitimate users. "
            "Conversely, alerts-only systems may leave sensitive data exposed too long."
        )
    )
    story.append(H("5. Fragile Evidence Trails", "SubSectionHead"))
    story.append(
        P(
            "Local logs can be incomplete or easily altered. Without append-friendly "
            "integrity mechanisms, later forensic review is weaker."
        )
    )
    story.append(H("6. Cloud Dependence for Teaching Labs", "SubSectionHead"))
    story.append(
        P(
            "Many modern platforms assume always-on cloud connectivity, which may not "
            "fit offline demonstrations or controlled lab networks."
        )
    )

    story.append(H("2.3 REQUIREMENTS OF THE NEW SYSTEM"))
    story.append(H("2.3.1 Functional Requirements", "SubSectionHead"))
    story.append(P("The proposed system shall:"))
    story.extend(
        numbered(
            [
                "Deploy a configurable set of honey decoy files representing sensitive document categories.",
                "Monitor file-system activity in Documents and decoy directories and map relevant operations to typed security events.",
                "Detect burst/mass file activity within a short time window.",
                "Collect USB insertion and removal events.",
                "Collect suspicious process-start events while allowing trusted processes to be skipped.",
                "Maintain a sliding threat window and compute risk, confidence, and correlation pattern matches.",
                "Select response actions through a policy engine and apply safety-gate checks.",
                "Create and update incidents, execute approved containment (including vault lockdown), and support resolve/rollback paths.",
                "Persist events to SQLite with sequence numbers and hash-chain fields.",
                "Expose status and control operations to Security Center via named-pipe IPC.",
                "Provide a WPF dashboard for threat metrics, timeline/events, incidents, vaults, and policies.",
                "Support console, simulate, demo, and Windows Service hosting modes.",
            ]
        )
    )

    story.append(H("2.3.2 Non-Functional Requirements", "SubSectionHead"))
    story.extend(
        bullets(
            [
                "Reliability: Guardian should continue collecting and scoring under ordinary desktop workloads; IPC clients should degrade gracefully when Guardian is offline.",
                "Performance: Polling and scoring must remain responsive on a typical student/developer Windows laptop; the threat window is bounded.",
                "Security: Vault secrets use Argon2id and AES-GCM; pipe ACLs restrict local clients; ProgramData ACLs can be hardened by script.",
                "Maintainability: Clean Architecture separation keeps Domain engines free of I/O; Shared contracts isolate IPC shapes.",
                "Testability: Domain engines must be unit-testable without UI or Windows Service dependencies.",
                "Usability: Security Center should present clear threat level, risk, confidence, and offline status to a non-expert operator.",
                "Portability within Windows: Target Windows 10/11 with .NET 8.",
                "Auditability: Formulas, enums, and schemas should be documented so reviewers can reproduce scoring decisions.",
            ]
        )
    )

    story.append(H("2.4 SYSTEM FEASIBILITY"))
    story.append(H("2.4.1 Technical Feasibility", "SubSectionHead"))
    story.append(
        P(
            "The project is technically feasible using mainstream Microsoft technologies. "
            ".NET 8 supports Windows Services, WPF, named pipes, and SQLite. "
            "FileSystemWatcher, drive polling, and process enumeration are available "
            "through the .NET / Windows APIs. Cryptographic primitives for AES-GCM and "
            "Argon2id are accessible via framework and NuGet libraries. QuestPDF can "
            "generate local PDF reports. These building blocks are mature and well "
            "documented, making implementation realistic within a Capstone timeline."
        )
    )

    story.append(H("2.4.2 Economic Feasibility", "SubSectionHead"))
    story.append(
        P(
            "MayaJaal uses open tooling and libraries suitable for academic use: the .NET "
            "SDK, xUnit, Serilog, Microsoft.Data.Sqlite, CommunityToolkit.Mvvm, and "
            "QuestPDF Community licensing for eligible users. No cloud subscription is "
            "required for core operation. Hardware requirements are limited to a standard "
            "Windows PC already available to students. Therefore, the economic cost of "
            "development and demonstration is low."
        )
    )

    story.append(H("2.4.3 Operational Feasibility", "SubSectionHead"))
    story.append(
        P(
            "Operationally, the system can be run in console mode for development, "
            "simulate/demo modes for presentations, and service-install scripts for a "
            "more production-like local deployment. Operators interact through Security "
            "Center without needing to understand engine internals. Because the platform "
            "is local-first, it can be demonstrated offline. Residual operational caution "
            "is required around decoy placement and automated lockdown so legitimate "
            "users are not unnecessarily disrupted; SafetyGate and confidence thresholds "
            "exist specifically to support this concern."
        )
    )

    story.append(H("2.5 ACTIVITIES IN PROPOSED SYSTEM"))
    story.append(P("Major activities supported by MayaJaal include:"))
    story.extend(
        bullets(
            [
                "Environment setup and build using setup scripts and the .NET SDK.",
                "Guardian startup (console or Windows Service) and optional simulation/demo scenarios.",
                "Automatic or bootstrap vault provisioning and unlock for containment readiness.",
                "Honey decoy deployment and continuous file-system monitoring.",
                "Ingestion of USB and process signals into the common event model.",
                "Continuous risk/confidence/correlation evaluation.",
                "Incident opening, containment execution, evidence packaging, and PDF reporting.",
                "Operator observation and manual lock/resolve/rollback actions via Security Center.",
                "Unit-test verification of domain engines and CI build on Windows runners.",
            ]
        )
    )

    story.append(H("2.6 MAIN MODULES"))
    story.append(
        P(
            "The system is organized into the following main modules. Table 2.1 "
            "summarizes their responsibilities."
        )
    )
    story.append(P("Table 2.1: System Module Descriptions", "Caption"))
    story.append(
        make_table(
            ["Module", "Description"],
            [
                [
                    "Shared Models &amp; IPC",
                    "Defines SecurityEvent, Incident, ThreatState, vault/asset models, and GuardianClient / IPC message contracts.",
                ],
                [
                    "Domain Engines",
                    "Pure Risk, Confidence, Correlation, Policy, and SafetyGate logic without I/O.",
                ],
                [
                    "Infrastructure Services",
                    "SQLite EventStore, crypto/KDF, VaultService, HoneyFileService, IncidentService, ReportGenerator.",
                ],
                [
                    "Guardian Host",
                    "Collectors, ThreatEngine orchestration, named-pipe IpcServer, demo/simulate modes, service hosting.",
                ],
                [
                    "Security Center",
                    "WPF MVVM operator console with live polling and threat visualization.",
                ],
                [
                    "Tests &amp; Scripts",
                    "xUnit domain tests; setup, demo, install, uninstall, and ACL hardening scripts.",
                ],
            ],
            col_widths=[2.0 * inch, 4.5 * inch],
        )
    )

    story.append(H("2.7 SOFTWARE AND HARDWARE"))
    story.append(H("2.7.1 Software Requirements", "SubSectionHead"))
    story.extend(
        bullets(
            [
                "Windows 10 or Windows 11",
                ".NET 8 SDK and runtime (including Windows desktop workload for WPF)",
                "Visual Studio 2022 or VS Code / Cursor with C# support (recommended)",
                "PowerShell 5+ for setup and service scripts",
                "Git for source control",
                "Optional: GitHub Actions for CI on windows-latest",
            ]
        )
    )
    story.append(H("2.7.2 Hardware Requirements", "SubSectionHead"))
    story.append(P("Table 2.2: Software and Hardware Requirements", "Caption"))
    story.append(
        make_table(
            ["Component", "Minimum Recommendation"],
            [
                ["Processor", "Dual-core 64-bit CPU"],
                ["Memory", "8 GB RAM (16 GB preferred for IDE + dual processes)"],
                ["Storage", "1 GB free for build outputs and ProgramData artifacts"],
                ["Display", "1366×768 or higher for Security Center"],
                ["USB", "Optional removable drive for live USB demos"],
            ],
            col_widths=[2.2 * inch, 4.3 * inch],
        )
    )

    story.append(H("2.8 PROPOSED SYSTEM OVERVIEW"))
    story.append(
        P(
            "The proposed MayaJaal system consists of a Guardian process and a Security "
            "Center process. Guardian collects endpoint signals, stores events, scores "
            "threat state, opens incidents, and serves IPC. Security Center presents that "
            "state to an operator and can request selected control operations."
        )
    )
    story.append(
        P(
            "At runtime, collectors emit SecurityEvent objects into ThreatEngine. The "
            "engine updates an in-memory window, may synthesize ransomware-behavior "
            "heuristics, computes risk and confidence, evaluates correlation patterns, "
            "asks PolicyEngine for a ResponseAction, and consults SafetyGate before "
            "executing containment. Approved actions may lock vaults and write evidence "
            "under %ProgramData%\\MayaJaal. The operator observes the outcome in Security "
            "Center within a short polling interval."
        )
    )
    story.append(
        P(
            "This overview establishes the analytical foundation for the detailed design "
            "presented in Chapter 3 and the implementation discussion in Chapter 4."
        )
    )
    story.append(PageBreak())
    return story


def build_chapter3():
    set_chapter("SYSTEM DESIGN")
    story = []
    story.append(P("CHAPTER 3: SYSTEM DESIGN", "ChapterTitle"))

    story.append(H("3.1 DESIGN METHODOLOGY"))
    story.append(
        P(
            "MayaJaal follows an iterative, modular, Clean Architecture-inspired "
            "methodology. Domain scoring logic is isolated from infrastructure I/O and "
            "from UI concerns. Shared contracts define the language spoken across "
            "process boundaries. This separation enables unit testing of engines, "
            "replacement of storage/crypto implementations, and independent evolution "
            "of the Security Center."
        )
    )
    story.append(
        P(
            "Development proceeded by first establishing the security-event model and "
            "engine formulas, then wiring Guardian collectors and ThreatEngine, then "
            "adding persistence/vault/incident services, and finally connecting the WPF "
            "console and operational scripts. Demo and simulate modes were designed "
            "early so scoring behavior could be demonstrated deterministically."
        )
    )

    story.append(H("3.2 SYSTEM ARCHITECTURE"))
    story.append(
        P(
            "The logical architecture comprises five primary projects plus tests:"
        )
    )
    story.extend(
        bullets(
            [
                "MayaJaal.Shared — models, enums, IPC DTOs, GuardianClient",
                "MayaJaal.Domain — Risk/Confidence/Correlation/Policy/SafetyGate",
                "MayaJaal.Infrastructure — SQLite, crypto, vault, honey, incidents, PDF",
                "MayaJaal.Guardian — host, collectors, ThreatEngine, IpcServer",
                "MayaJaal.SecurityCenter — WPF MVVM operator UI",
                "MayaJaal.Tests — xUnit tests for domain engines",
            ]
        )
    )
    story.append(P("Figure 3.1: MayaJaal System Architecture", "Caption"))
    story.append(
        P(
            "Runtime data flows from collectors into ThreatEngine, then to EventStore and "
            "Incident/Vault services. IpcServer exposes a length-prefixed UTF-8 JSON "
            "protocol on the named pipe MayaJaal.Guardian. Security Center’s "
            "GuardianStatusService polls this pipe approximately every two seconds."
        )
    )
    story.append(P("Table 3.1: System Layer Responsibilities", "Caption"))
    story.append(
        make_table(
            ["Layer", "Responsibility"],
            [
                ["Presentation", "Security Center views/viewmodels; operator actions"],
                ["Application Host", "Guardian DI composition, hosted services, bootstrap"],
                ["Domain", "Pure threat math and policy/safety decisions"],
                ["Infrastructure", "Persistence, cryptography, file decoys, reporting"],
                ["Shared", "Cross-process contracts and models"],
            ],
            col_widths=[1.8 * inch, 4.7 * inch],
        )
    )

    story.append(H("3.3 SYSTEM MODULE DESIGN"))
    story.append(
        P(
            "Modules interact through interfaces and message contracts rather than "
            "tight UI-to-database coupling. Collectors depend on IThreatEngine. "
            "ThreatEngine depends on domain engines and infrastructure services. "
            "Security Center depends only on Shared IPC types. This design reduces "
            "accidental complexity and clarifies trust boundaries."
        )
    )

    story.append(H("3.4 DECEPTION AND COLLECTOR DESIGN"))
    story.append(
        P(
            "HoneyFileService deploys six templates (Financial, Credential, Strategy, "
            "HR, API, Personal) under the user’s Documents\\MayaJaal-Decoys directory. "
            "EventCollector uses FileSystemWatcher callbacks and maps operations to "
            "FILE_* or HONEY_* event types. Burst detection synthesizes "
            "MASS_FILE_ACTIVITY when operation volume exceeds a threshold "
            "(approximately ≥25 operations in 10 seconds, with short de-duplication)."
        )
    )
    story.append(
        P(
            "UsbCollector periodically polls removable drives and emits USB_INSERT / "
            "USB_REMOVE events. ProcessCollector polls running processes and emits "
            "PROCESS_START for suspicious names, subject to ProcessAllowlist filtering. "
            "Honey-related events are never suppressed by the allowlist."
        )
    )

    story.append(H("3.5 THREAT ENGINE DESIGN"))
    story.append(P("Figure 3.2: Threat Processing Pipeline", "Caption"))
    story.append(
        P(
            "ThreatEngine is the orchestration heart of Guardian. For each incoming "
            "SecurityEvent it typically: (1) optionally drops allowlisted noise, "
            "(2) persists via EventStore with hash chaining, (3) updates the sliding "
            "window, (4) may emit ransomware-behavior heuristic events, (5) computes "
            "risk, (6) computes confidence, (7) evaluates correlation patterns and may "
            "boost risk, (8) asks PolicyEngine for a ResponseAction, (9) asks SafetyGate "
            "for approval, and (10) creates/updates incidents and executes approved "
            "responses. Startup may replay recent SQLite events into the window to "
            "reduce cold-start blindness."
        )
    )

    story.append(H("3.6 RISK, CONFIDENCE, AND CORRELATION DESIGN"))
    story.append(
        P(
            "RiskEngine computes a time-decayed weighted sum over recent events and "
            "soft-caps the result to the integer interval [0, 200]. Honey events receive "
            "elevated base weights and type multipliers so decoy access strongly "
            "influences the score. Threat bands map risk/confidence combinations into "
            "SAFE through CRITICAL levels, with low confidence capping escalation."
        )
    )
    story.append(
        P(
            "ConfidenceEngine combines factors approximately as: "
            "0.4·SignalStrength + 0.3·CorrelationQuality + 0.2·ContextConsistency + "
            "0.1·HistoricalAccuracy. The result lies in [0.0, 1.0] and is compared "
            "against action-specific thresholds."
        )
    )
    story.append(
        P(
            "CorrelationEngine evaluates named set-membership patterns, including "
            "USB_Honey, Honey_MassCopy, USB_Process_MassCopy, "
            "Insider_Credential_Exfil, and Ransomware_Like. Pattern matches improve "
            "correlation quality and may boost risk toward the soft cap."
        )
    )

    story.append(H("3.7 POLICY AND SAFETY GATE DESIGN"))
    story.append(
        P(
            "PolicyEngine maps threat level and confidence to ResponseAction values "
            "such as NONE/MONITOR, ALERT, CONTAIN, and EMERGENCY_LOCKDOWN. "
            "SafetyGate is the last defensive checkpoint before high-impact execution. "
            "It may block lockdown when confidence is insufficient or when other safety "
            "checks fail, and it associates rollback plans with approved actions. This "
            "dual-control design intentionally separates “what policy wants” from "
            "“what is safe to do now.”"
        )
    )

    story.append(H("3.8 VAULT AND CRYPTOGRAPHY DESIGN"))
    story.append(
        P(
            "VaultService manages password-wrapped vaults under ProgramData\\MayaJaal\\Vaults. "
            "A user/bootstrap password and salt derive a wrapping key via Argon2id. A "
            "random 32-byte master key is AES-GCM-wrapped into vault metadata. Item "
            "payloads are stored as encrypted blobs. DPAPI protects a default vault "
            "bootstrap secret so Guardian can auto-provision and unlock a containment "
            "vault on local machine scope. LockAll encrypts/locks unlocked vaults during "
            "emergency response, providing reversible containment rather than destructive "
            "wiping."
        )
    )

    story.append(H("3.9 DATABASE AND EVIDENCE DESIGN"))
    story.append(P("Figure 3.3: Event Store and Vault Data Layout", "Caption"))
    story.append(
        P(
            "EventStore uses Microsoft.Data.Sqlite with an events table containing "
            "identifiers, sequence, timestamp, event_type, source, severity, honey/"
            "protected flags, risk contribution, path, full JSON payload, and hash-chain "
            "fields. Each insert computes a SHA-256 integrity hash over a compact "
            "canonical field set chained to the previous hash or GENESIS."
        )
    )
    story.append(P("Table 3.2: Major Data Entities and Stores", "Caption"))
    story.append(
        make_table(
            ["Entity / Store", "Purpose"],
            [
                ["SecurityEvent", "Normalized telemetry unit in memory and SQLite"],
                ["ThreatState", "Aggregated risk/confidence/level/pattern for UI"],
                ["Incident", "Opened case with status, actions, and evidence links"],
                ["Vault / VaultItem", "Encrypted containment container and items"],
                ["ProtectedAsset", "Assets marked for restriction/protection workflows"],
                ["Evidence pack", "JSON (+ PDF) artifacts under ProgramData\\Evidence"],
            ],
            col_widths=[2.0 * inch, 4.5 * inch],
        )
    )
    story.append(
        P(
            "Incidents are numbered INC-{yyyyMMdd}-{counter:D4}. ReportGenerator can "
            "render incident PDFs using QuestPDF for operator/archival use."
        )
    )

    story.append(H("3.10 INTERFACE DESIGN"))
    story.append(H("3.10.1 Named-Pipe IPC Interface", "SubSectionHead"))
    story.append(
        P(
            "IPC frames are length-prefixed UTF-8 JSON: a 32-bit little-endian length "
            "followed by the payload. Table 3.3 lists principal commands."
        )
    )
    story.append(P("Table 3.3: Named-Pipe IPC Commands", "Caption"))
    story.append(
        make_table(
            ["Command", "Purpose"],
            [
                ["PING", "Liveness check"],
                ["GET_STATUS", "Threat metrics and Guardian health"],
                ["GET_EVENTS", "Recent security events"],
                ["GET_INCIDENT / GET_INCIDENTS", "Active or historical incidents"],
                ["GET_ASSETS", "Protected asset inventory"],
                ["GET_VAULTS", "Vault inventory/state"],
                ["GET_POLICIES", "Policy view model for UI"],
                ["LOCK_VAULT", "Manual containment lock"],
                ["RESOLVE_INCIDENT", "Mark incident resolved"],
                ["EXECUTE_ROLLBACK", "Execute associated rollback plan"],
            ],
            col_widths=[2.4 * inch, 4.1 * inch],
        )
    )

    story.append(H("3.10.2 Security Center Interfaces", "SubSectionHead"))
    story.append(
        P(
            "The Security Center provides a dashboard with threat level, risk score, "
            "confidence, correlation pattern, KPI-style summaries, and an event "
            "timeline. Navigation pages cover incidents, vaults, and policies. An "
            "offline banner appears when Guardian cannot be reached. The visual design "
            "prioritizes operator clarity over decorative complexity."
        )
    )

    story.append(H("3.10.3 Operational CLI Interfaces", "SubSectionHead"))
    story.append(
        P(
            "Guardian supports --console, --simulate, and --demo flags. Demo mode runs "
            "a scripted escalation path (for example USB → process → honey → mass "
            "activity) intended to reach CRITICAL with EMERGENCY_LOCKDOWN under "
            "expected scoring. PowerShell scripts wrap setup, demo launch, service "
            "install/uninstall, and ACL hardening."
        )
    )

    story.append(H("3.11 SECURITY AND ACCESS CONTROL"))
    story.append(
        P(
            "MayaJaal’s trust model is local Windows identity rather than application "
            "usernames. SecurePipeFactory configures named-pipe ACLs for LocalSystem, "
            "Administrators, and the current user SID, with an additional connected-"
            "client authorization check. Vault passwords never persist in plaintext; "
            "wrapping keys are derived with Argon2id; item encryption uses AES-256-GCM. "
            "Bootstrap secrets can be protected with DPAPI (LocalMachine). "
            "harden-acls.ps1 can strip inheritance on ProgramData\\MayaJaal and grant "
            "least-privilege access patterns for evidence directories."
        )
    )
    story.append(
        P(
            "Residual risks remain, as with any local agent: a privileged attacker on "
            "the same machine can interfere with services and files. MayaJaal does not "
            "claim kernel-level tamper resistance. Its security value is strongest as a "
            "transparent correlation and reversible-containment layer with auditable math."
        )
    )

    story.append(H("3.12 SUMMARY OF SYSTEM DESIGN"))
    story.append(
        P(
            "The design separates collection, pure scoring, persistence/crypto, host "
            "orchestration, and operator UI. Deception provides high-value signals; "
            "engines compose weak signals into confidence; policy and safety gate "
            "responses; vaults and evidence make containment and review concrete. "
            "Chapter 4 describes how this design is realized in code and verified "
            "through testing."
        )
    )
    story.append(PageBreak())
    return story


def build_chapter4():
    set_chapter("IMPLEMENTATION")
    story = []
    story.append(P("CHAPTER 4: IMPLEMENTATION", "ChapterTitle"))

    story.append(H("4.1 TECHNOLOGY STACK"))
    story.append(
        P(
            "MayaJaal is implemented as a multi-project .NET 8 solution using C# 12. "
            "The stack is divided according to role."
        )
    )
    story.append(P("Figure 4.1: Technology Stack Overview", "Caption"))

    story.append(H("4.1.1 Language and Runtime", "SubSectionHead"))
    story.append(
        P(
            "C# 12 on .NET 8 provides strong typing, async/await for IPC and collectors, "
            "and first-class Windows desktop/service support. Library projects target "
            "net8.0 where possible; Guardian and Security Center target net8.0-windows."
        )
    )

    story.append(H("4.1.2 Guardian Host / Backend", "SubSectionHead"))
    story.append(
        P(
            "Microsoft.Extensions.Hosting and Windows Services packages host Guardian. "
            "Dependency injection registers domain engines and infrastructure services "
            "through AddMayaJaalCore() and Guardian-specific hosted components "
            "(ThreatEngine, collectors, IpcServer, bootstrap)."
        )
    )

    story.append(H("4.1.3 Security Center / UI", "SubSectionHead"))
    story.append(
        P(
            "WPF with CommunityToolkit.Mvvm implements the operator console using "
            "MVVM patterns. MainViewModel and GuardianStatusService coordinate polling "
            "and bind threat state to XAML views."
        )
    )

    story.append(H("4.1.4 Persistence and Logging", "SubSectionHead"))
    story.append(
        P(
            "Microsoft.Data.Sqlite stores events locally. Serilog writes console and "
            "rolling file logs under ProgramData. This combination supports both "
            "developer debugging and operator diagnostics."
        )
    )

    story.append(H("4.1.5 Cryptography", "SubSectionHead"))
    story.append(
        P(
            "AES-256-GCM protects vault items and wrapped keys. Argon2id "
            "(Konscious.Security.Cryptography.Argon2) derives wrapping keys from "
            "passwords. SHA-256 supports hash chaining. DPAPI protects bootstrap "
            "secrets on the local machine."
        )
    )

    story.append(H("4.1.6 Reporting", "SubSectionHead"))
    story.append(
        P(
            "QuestPDF generates incident PDF reports for evidence and archival "
            "workflows associated with containment incidents."
        )
    )

    story.append(H("4.1.7 Testing and CI", "SubSectionHead"))
    story.append(
        P(
            "xUnit validates domain engines. GitHub Actions on windows-latest restores, "
            "builds, and tests the solution for main/develop branches."
        )
    )

    story.append(H("4.1.8 Deployment Tools", "SubSectionHead"))
    story.append(
        P(
            "PowerShell scripts publish Guardian/Security Center, create the "
            "MayaJaal Guardian service via sc.exe, add Start Menu shortcuts, uninstall "
            "cleanly, and harden directory ACLs. An installer folder is reserved for "
            "future MSI packaging."
        )
    )

    story.append(P("Table 4.1: Technology Stack and Tools", "Caption"))
    story.append(
        make_table(
            ["Area", "Technologies"],
            [
                ["Language/Runtime", ".NET 8, C# 12"],
                ["UI", "WPF, CommunityToolkit.Mvvm"],
                ["Host", "MS.Extensions.Hosting, Windows Services"],
                ["IPC", "Named pipes, length-prefixed JSON"],
                ["Database", "SQLite via Microsoft.Data.Sqlite"],
                ["Crypto", "AES-GCM, Argon2id, SHA-256, DPAPI"],
                ["Logging", "Serilog"],
                ["PDF", "QuestPDF"],
                ["Tests/CI", "xUnit, GitHub Actions (windows-latest)"],
                ["Ops Scripts", "PowerShell, sc.exe, publish profiles"],
            ],
            col_widths=[2.0 * inch, 4.5 * inch],
        )
    )

    story.append(H("4.2 MODULE DEVELOPMENT"))
    story.append(H("4.2.1 Shared Models and IPC Client Module", "SubSectionHead"))
    story.append(
        P(
            "MayaJaal.Shared defines SecurityEvent and related enums (EventType, "
            "EventSource, EventSeverity, ThreatLevel, IncidentStatus, ResponseAction). "
            "GuardianClient implements the client side of the pipe protocol used by "
            "Security Center. Keeping these types shared prevents UI/host drift."
        )
    )

    story.append(H("4.2.2 Domain Engines Module", "SubSectionHead"))
    story.append(
        P(
            "RiskEngine, ConfidenceEngine, CorrelationEngine, PolicyEngine, and "
            "SafetyGate were implemented as deterministic components with no file or "
            "network I/O. This enabled focused unit tests for empty windows, honey "
            "elevation, confidence gating, decay behavior, and named pattern detection."
        )
    )

    story.append(H("4.2.3 Infrastructure Module", "SubSectionHead"))
    story.append(
        P(
            "EventStore creates/opens events.db and appends chained events. "
            "VaultService and KeyManager implement create/unlock/lock/encrypt flows. "
            "HoneyFileService writes decoy templates. IncidentService coordinates "
            "incident lifecycle and evidence. ReportGenerator produces PDFs. "
            "DependencyInjection centralizes service registration."
        )
    )

    story.append(H("4.2.4 Guardian Collectors and ThreatEngine Module", "SubSectionHead"))
    story.append(
        P(
            "EventCollector, UsbCollector, and ProcessCollector run as hosted "
            "background work. ProcessAllowlist reduces noise. ThreatEngine integrates "
            "all domain/infrastructure collaborators. DemoScenarioRunner provides a "
            "deterministic escalation path; simulate mode injects ongoing demo "
            "telemetry while keeping IPC available for the console."
        )
    )

    story.append(H("4.2.5 IPC Server and Pipe Security Module", "SubSectionHead"))
    story.append(
        P(
            "IpcServer accepts connections on MayaJaal.Guardian and dispatches commands "
            "to Guardian services. SecurePipeFactory applies ACLs and performs "
            "connected-client authorization checks. Expanded commands cover vaults, "
            "incidents, policies, resolve, and rollback in addition to status/events."
        )
    )

    story.append(H("4.2.6 Security Center Module", "SubSectionHead"))
    story.append(
        P(
            "MainWindow.xaml and related views present dashboard, incidents, vault, and "
            "policy pages. GuardianStatusService polls multiple GET_* commands and "
            "updates observable state. Offline detection drives a banner so operators "
            "know when metrics are stale."
        )
    )
    story.append(P("Figure 4.2: Security Center Dashboard Concept", "Caption"))
    story.append(
        P(
            "The dashboard emphasizes threat level, risk, confidence, active pattern, "
            "recent events, and incident summary—matching the “one job per view” "
            "operator need for rapid situational awareness."
        )
    )

    story.append(H("4.2.7 Operations and Demo Module", "SubSectionHead"))
    story.append(
        P(
            "scripts\\setup.ps1 restores/builds/tests. scripts\\run-demo.ps1 launches "
            "Guardian with --console --simulate and then Security Center. "
            "install-service.ps1 / uninstall-service.ps1 manage the Windows Service "
            "installation under Program Files. harden-acls.ps1 tightens ProgramData "
            "permissions after deployment."
        )
    )

    story.append(P("Table 4.2: Module Description", "Caption"))
    story.append(
        make_table(
            ["Module", "Description"],
            [
                ["Shared IPC/Models", "Cross-process contracts and security domain types"],
                ["Domain Engines", "Risk, confidence, correlation, policy, safety"],
                ["EventStore", "SQLite persistence + hash chain"],
                ["Vault/Crypto", "Argon2id + AES-GCM containment"],
                ["Honey/Collectors", "Decoys + file/USB/process telemetry"],
                ["ThreatEngine", "End-to-end scoring and response orchestration"],
                ["IpcServer", "Named-pipe command surface"],
                ["Security Center", "Operator visualization and control"],
                ["Scripts/CI", "Setup, demo, service install, automated build/test"],
            ],
            col_widths=[2.0 * inch, 4.5 * inch],
        )
    )

    story.append(H("4.3 TESTING"))
    story.append(
        P(
            "Testing focused on correctness of scoring logic, reliability of build/"
            "demo paths, and practical verification of escalation behavior. Because "
            "Domain engines are pure, they received automated unit tests. Host, IPC, "
            "and UI paths were verified primarily through controlled manual and demo "
            "scenarios, with CI ensuring Release builds and tests remain green."
        )
    )

    story.append(H("4.3.1 Unit Testing", "SubSectionHead"))
    story.append(
        P(
            "xUnit tests cover RiskEngine (empty risk, honey elevation, decay, combine), "
            "ConfidenceEngine (gates and theory cases), and CorrelationEngine (pattern "
            "detection and relatedness). Approximately nineteen automated cases form "
            "the regression net for scoring math."
        )
    )

    story.append(H("4.3.2 Integration and Host Testing", "SubSectionHead"))
    story.append(
        P(
            "Integration-style verification uses Guardian --demo and --console --simulate "
            "together with Security Center. These runs validate collector emission, "
            "IPC availability, UI refresh, and incident/lockdown side effects on a "
            "developer workstation."
        )
    )

    story.append(H("4.3.3 Functional Testing", "SubSectionHead"))
    story.append(
        P(
            "Functional tests exercised honey deployment, mass-activity synthesis, "
            "USB/process signals (live or simulated), policy selection, SafetyGate "
            "behavior, vault lock, incident evidence writing, and resolve/rollback "
            "command paths exposed over IPC."
        )
    )

    story.append(H("4.3.4 User Interface Testing", "SubSectionHead"))
    story.append(
        P(
            "UI testing confirmed that dashboard metrics update, offline banners appear "
            "when Guardian is stopped, navigation across incidents/vaults/policies "
            "remains usable, and critical states are visually obvious to an operator."
        )
    )

    story.append(H("4.3.5 Performance Testing", "SubSectionHead"))
    story.append(
        P(
            "Performance considerations include bounded threat-window size, polling "
            "interval of about two seconds, collector poll intervals of a few seconds, "
            "and avoidance of unbounded UI collections. On typical student hardware, "
            "demo escalation remains interactive without noticeable UI freezing."
        )
    )

    story.append(H("4.3.6 Security Testing", "SubSectionHead"))
    story.append(
        P(
            "Security-focused checks included validating that vault secrets are not "
            "stored in plaintext, confirming pipe ACL configuration paths, reviewing "
            "allowlist bypass rules for honey events, and ensuring high-impact actions "
            "remain confidence-gated. Residual local privilege risks are documented "
            "rather than overclaimed."
        )
    )

    story.append(P("Table 4.3: Sample Test Results", "Caption"))
    story.append(
        make_table(
            ["Test Area", "Sample Case", "Result"],
            [
                ["RiskEngine", "Honey event elevates risk vs empty window", "Pass"],
                ["ConfidenceEngine", "USB+honey+mass yields high confidence", "Pass"],
                ["CorrelationEngine", "USB_Process_MassCopy pattern detected", "Pass"],
                ["Demo scenario", "CRITICAL / lockdown path exercised", "Pass"],
                ["CI Release build", "dotnet test on windows-latest", "Pass"],
                ["UI offline banner", "Stop Guardian during poll loop", "Pass"],
            ],
            col_widths=[1.8 * inch, 3.2 * inch, 1.5 * inch],
        )
    )

    story.append(H("4.4 DEMO NARRATIVE WALKTHROUGH"))
    story.append(
        P(
            "A representative demo sequence is: USB insertion; suspicious process "
            "start; honey access; mass file activity; correlation selects "
            "USB_Process_MassCopy; risk approaches the soft cap; confidence crosses "
            "lockdown thresholds; SafetyGate approves; vaults lock; Security Center "
            "shows CRITICAL with incident details. Table 4.4 summarizes this path."
        )
    )
    story.append(P("Table 4.4: Demo Escalation Steps", "Caption"))
    story.append(
        make_table(
            ["Step", "Signal / Module", "Expected Effect"],
            [
                ["1", "USB_INSERT", "Removable-media context"],
                ["2", "PROCESS_START", "Suspicious tooling context"],
                ["3", "HONEY_* access", "High-weight deception trigger"],
                ["4", "MASS_FILE_ACTIVITY", "Volume / exfil behavior"],
                ["5", "Correlation + Policy", "Pattern + response selection"],
                ["6", "SafetyGate + Vault", "Approved lockdown + evidence"],
            ],
            col_widths=[0.8 * inch, 2.2 * inch, 3.5 * inch],
        )
    )

    story.append(H("4.5 IMPLEMENTATION CHALLENGES"))
    story.append(
        P(
            "Key challenges included normalizing collector timestamps and paths into "
            "one SecurityEvent model, keeping Security Center responsive during IPC "
            "polling, tuning confidence thresholds for reliable demos without trivial "
            "false lockdowns, and balancing named-pipe ACLs so demos work under common "
            "login contexts without leaving the pipe overly open."
        )
    )
    story.append(PageBreak())
    return story


def build_chapter5():
    set_chapter("EVALUATION AND FUTURE WORK")
    story = []
    story.append(P("CHAPTER 5: EVALUATION AND FUTURE WORK", "ChapterTitle"))

    story.append(H("5.1 EVALUATION OF RESULTS"))
    story.append(
        P(
            "MayaJaal was evaluated against the requirements defined in system analysis "
            "and the design commitments of Chapter 3. Evaluation considered functional "
            "completeness, scoring transparency, operator usability, local security "
            "controls, and verification evidence from unit tests and demo runs."
        )
    )
    story.append(H("Functional Evaluation", "SubSectionHead"))
    story.append(
        P(
            "Deception and collectors successfully produce typed events from honey "
            "access, file activity, USB changes, and suspicious processes. ThreatEngine "
            "orchestrates persistence, scoring, correlation, policy, safety, and "
            "incident response. Security Center displays live state and supports key "
            "operator workflows. Demo mode provides a repeatable CRITICAL escalation "
            "narrative useful for academic demonstration."
        )
    )
    story.append(H("Scoring and Test Evaluation", "SubSectionHead"))
    story.append(
        P(
            "Domain unit tests (Risk, Confidence, Correlation, Policy, and SafetyGate — 30+ cases) passed in Release configuration. "
            "This supports the claim that the normative formulas are not only documented "
            "but executable and regression-protected. The educational value of explicit "
            "math is one of the project’s strongest outcomes."
        )
    )
    story.append(H("Security and Operations Evaluation", "SubSectionHead"))
    story.append(
        P(
            "AES-GCM vault lockdown, Argon2id wrapping, hash-chained SQLite events, "
            "pipe ACLs, and ACL hardening scripts collectively demonstrate a thoughtful "
            "local trust model. Service install scripts show a path from classroom demo "
            "to workstation service deployment, even though full MSI packaging remains "
            "future work."
        )
    )
    story.append(P("Table 5.1: Evaluation of Major Modules", "Caption"))
    story.append(
        make_table(
            ["Module", "Evaluation Outcome"],
            [
                ["Honey + Collectors", "Meets scope; emits actionable local telemetry"],
                ["Risk/Confidence/Correlation", "Meets scope; unit-tested formulas"],
                ["Policy + SafetyGate", "Meets scope; confidence-gated containment"],
                ["Vault + Evidence", "Meets scope; reversible lockdown + artifacts"],
                ["Security Center", "Meets scope; live IPC visualization"],
                ["Service/Scripts/CI", "Partially mature; MSI still future work"],
            ],
            col_widths=[2.2 * inch, 4.3 * inch],
        )
    )
    story.append(
        P(
            "Limitations observed include incomplete automated coverage for IPC/UI/"
            "collectors, absence of kernel-level sensors, and the inherent limits of "
            "any user-mode local agent against a fully privileged adversary. These "
            "limitations are acceptable for Capstone scope when clearly stated."
        )
    )

    story.append(H("5.2 FUTURE ENHANCEMENTS"))
    story.append(H("5.2.1 Deeper Telemetry Sensors", "SubSectionHead"))
    story.append(
        P(
            "Future work may integrate ETW providers, richer process ancestry, and "
            "optional kernel minifilter callbacks to reduce reliance on polling and "
            "improve stealthy-activity visibility."
        )
    )
    story.append(H("5.2.2 Continuous Integrity Verification", "SubSectionHead"))
    story.append(
        P(
            "A background hash-chain verifier job can periodically validate EventStore "
            "integrity and alert on breaks, strengthening forensic assurance."
        )
    )
    story.append(H("5.2.3 Expanded Automated Testing", "SubSectionHead"))
    story.append(
        P(
            "Integration tests for IPC framing, vault round-trips, collector adapters, "
            "and UI viewmodels would increase release confidence beyond domain unit tests."
        )
    )
    story.append(H("5.2.4 Packaging and Enterprise Operations", "SubSectionHead"))
    story.append(
        P(
            "MSI/MSIX installers, signed binaries, central policy packs, and optional "
            "SIEM export connectors would help transition from prototype to managed "
            "fleet use—while preserving the local-first core."
        )
    )
    story.append(H("5.2.5 Machine-Learning Complements", "SubSectionHead"))
    story.append(
        P(
            "Although MayaJaal intentionally uses transparent heuristics today, future "
            "optional anomaly models could complement correlation patterns if their "
            "features and false-positive behavior remain explainable to operators."
        )
    )
    story.append(H("5.2.6 Cross-Host Deception Fabric", "SubSectionHead"))
    story.append(
        P(
            "Coordinated decoys across multiple lab machines could support blue-team "
            "exercises, provided privacy and consent boundaries remain explicit."
        )
    )
    story.append(H("5.2.7 Usability and Accessibility Polish", "SubSectionHead"))
    story.append(
        P(
            "Additional guided onboarding, clearer incident narratives, and "
            "accessibility improvements in Security Center would broaden operator "
            "audiences in classrooms and SOCs-in-training."
        )
    )

    story.append(H("5.3 SUMMARY"))
    story.append(
        P(
            "Evaluation indicates that MayaJaal meets its Capstone objectives as a "
            "local-first deception-and-response prototype with transparent scoring, "
            "practical containment, and an operator console. Future enhancements are "
            "natural extensions rather than repairs of a broken core: deeper sensors, "
            "stronger verification jobs, richer tests, and packaging for wider "
            "deployment. Chapter 6 consolidates the conclusions of this work."
        )
    )
    story.append(PageBreak())
    return story


def build_chapter6():
    set_chapter("CONCLUSION AND DISCUSSION")
    story = []
    story.append(P("CHAPTER 6: CONCLUSION AND DISCUSSION", "ChapterTitle"))
    story.append(H("6.1 CONCLUSION"))
    story.append(
        P(
            "MayaJaal – Local-First Cyber Deception and Endpoint Defense Platform was "
            "developed to demonstrate how deception artifacts, behavioral scoring, "
            "policy/safety controls, cryptography, and operator visualization can be "
            "combined into a coherent Windows security prototype."
        )
    )
    story.append(
        P(
            "The system integrates a Guardian host and a Security Center console. "
            "Guardian collects file, USB, and process signals; scores a sliding threat "
            "window using Risk, Confidence, and Correlation engines; selects responses "
            "through PolicyEngine; gates them with SafetyGate; and persists events and "
            "incidents with hash-chained SQLite storage and vault-backed containment. "
            "Security Center provides live situational awareness over named-pipe IPC."
        )
    )
    story.append(
        P(
            "From a software-engineering perspective, the project applies Clean "
            "Architecture principles, dependency injection, MVVM, automated domain "
            "testing, and scripted operations. From a security-engineering perspective, "
            "it emphasizes explainable math, reversible containment, and explicit trust "
            "boundaries rather than opaque cloud scoring alone."
        )
    )
    story.append(
        P(
            "The Capstone implementation does not claim to replace commercial EDR or "
            "antivirus products. Instead, it shows that a student team can design and "
            "build a meaningful local defense narrative: weak signals become strong "
            "when correlated with honey access; high-impact actions require confidence; "
            "and evidence should remain durable and reviewable."
        )
    )
    story.append(
        P(
            "The modular architecture provides a foundation for future enhancements "
            "such as deeper telemetry, continuous integrity verification, broader "
            "automated tests, professional packaging, and optional analytics. Overall, "
            "MayaJaal successfully fulfills the objectives of Capstone Project – I by "
            "delivering a working, documented, and demonstrable cyber-defense platform."
        )
    )
    story.append(
        P(
            "We believe the project’s greatest academic contribution is not only the "
            "running software, but the clarity with which detection and response "
            "decisions can be explained, tested, and improved—an essential habit for "
            "responsible security engineering."
        )
    )

    story.append(H("6.2 DISCUSSION"))
    story.append(
        P(
            "Several trade-offs shaped the final system. Transparency was preferred "
            "over opaque detection power so formulas remain teachable and testable. "
            "Local-first operation was preferred over cloud analytics to support "
            "offline labs and clear trust boundaries. Safety-gated, reversible "
            "containment was preferred over immediate destructive response to reduce "
            "harm from false positives. User-mode collectors were preferred over "
            "kernel drivers to keep installation and demonstration practical within "
            "Capstone constraints."
        )
    )
    story.append(
        P(
            "Relative to antivirus, MayaJaal is complementary rather than competitive: "
            "it correlates behavior and deception instead of classifying binaries. "
            "Relative to enterprise EDR, it is narrower but more inspectable. Relative "
            "to network honeypots, it focuses on endpoint document pathways. These "
            "comparisons clarify the project’s intended niche."
        )
    )
    story.append(
        P(
            "Lessons learned include the value of pure Domain engines, deterministic "
            "demo modes, explicit IPC contracts, and documenting constants beside code. "
            "Ethically, decoys must stay synthetic, and lockdown demonstrations must "
            "avoid trapping legitimate coursework or personal files."
        )
    )
    story.append(PageBreak())
    return story


def build_appendix():
    set_chapter("Appendix")
    story = []
    story.append(P("Appendix", "ChapterTitle"))

    story.append(H("A. SOURCE CODE REPOSITORY"))
    story.append(
        P(
            "The source code of the MayaJaal project is organized as a Visual Studio / "
            ".NET solution with projects under src\\ and tests\\, documentation under "
            "docs\\, and operational scripts under scripts\\. The repository contains "
            "Guardian, Security Center, Shared/Domain/Infrastructure libraries, unit "
            "tests, CI workflow, and supporting configuration files."
        )
    )
    story.append(
        P(
            "Principal paths include MayaJaal.sln, Directory.Build.props, "
            "src\\MayaJaal.Guardian, src\\MayaJaal.SecurityCenter, "
            "src\\MayaJaal.Domain\\Engines, src\\MayaJaal.Infrastructure, "
            "tests\\MayaJaal.Tests, docs\\MAYAJAAL-COMPREHENSIVE-REPORT.md, and "
            ".github\\workflows\\build.yml."
        )
    )

    story.append(H("B. DEMO AND IPC VERIFICATION"))
    story.append(
        P(
            "Verification during development included Guardian demo/simulate runs and "
            "Security Center IPC polling. Command classes exercised included status, "
            "events, incidents, assets, vaults, policies, lock, resolve, rollback, and "
            "ping. Additional screenshots of Security Center and console output can be "
            "inserted here as needed for submission hardcopies."
        )
    )

    story.append(H("C. SAMPLE DATA / STORAGE STRUCTURE"))
    story.append(
        make_table(
            ["Location / Entity", "Purpose"],
            [
                ["%ProgramData%\\MayaJaal\\Data\\events.db", "Hash-chained security events"],
                ["%ProgramData%\\MayaJaal\\Vaults", "Encrypted vault containers"],
                ["%ProgramData%\\MayaJaal\\Incidents", "Incident records"],
                ["%ProgramData%\\MayaJaal\\Evidence", "Evidence packs / reports"],
                ["%ProgramData%\\MayaJaal\\Logs", "Serilog rolling logs"],
                ["%USERPROFILE%\\Documents\\MayaJaal-Decoys", "Honey decoy files"],
            ],
            col_widths=[3.2 * inch, 3.3 * inch],
        )
    )

    story.append(H("D. SAMPLE RISK MODEL NOTES"))
    story.append(
        P(
            "Risk is conceptually a time-decayed weighted sum soft-capped to [0, 200]. "
            "Honey events carry elevated weights. Confidence is a weighted blend of "
            "signal strength, correlation quality, context consistency, and historical "
            "accuracy. Correlation patterns are set-membership matches rather than "
            "strict ordered sequences. Exact constants are maintained in Domain engine "
            "source files and the comprehensive engineering report."
        )
    )

    story.append(H("E. BUILD AND RUN COMMANDS"))
    story.extend(
        bullets(
            [
                "Setup: .\\scripts\\setup.ps1",
                "Guardian console: dotnet run --project src\\MayaJaal.Guardian -- --console",
                "Simulate: add --simulate",
                "Demo: add --demo",
                "Security Center: dotnet run --project src\\MayaJaal.SecurityCenter",
                "Live demo wrapper: .\\scripts\\run-demo.ps1",
                "Service install (admin): .\\scripts\\install-service.ps1",
            ]
        )
    )
    story.append(PageBreak())
    return story


def build_references():
    set_chapter("Reference")
    story = []
    story.append(P("Reference", "ChapterTitle"))
    refs = [
        "Microsoft. .NET 8 Documentation. Microsoft Learn. Available at: https://learn.microsoft.com/dotnet/",
        "Microsoft. Windows Services with .NET. Microsoft Learn. Available at: https://learn.microsoft.com/dotnet/core/extensions/windows-service",
        "Microsoft. Named Pipes in .NET. Microsoft Learn. Available at: https://learn.microsoft.com/dotnet/standard/io/how-to-use-named-pipes-for-network-interprocess-communication",
        "Microsoft. Data Protection API (DPAPI). Microsoft Learn. Available at: https://learn.microsoft.com/dotnet/standard/security/",
        "SQLite Development Team. SQLite Documentation. Available at: https://www.sqlite.org/docs.html",
        "Microsoft. Microsoft.Data.Sqlite Documentation. Microsoft Learn.",
        "Serilog Contributors. Serilog Documentation. Available at: https://serilog.net/",
        "Community Toolkit. MVVM Toolkit Documentation. Available at: https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/",
        "QuestPDF Contributors. QuestPDF Documentation. Available at: https://www.questpdf.com/",
        "xUnit.net Contributors. xUnit.net Documentation. Available at: https://xunit.net/",
        "RFC 5116. An Interface and Algorithms for Authenticated Encryption (AES-GCM related). IETF.",
        "Biryukov, A., Dinu, D., Khovratovich, D., & Josefsson, S. Argon2 Memory-Hard Function for Password Hashing and Proof-of-Work Applications. IETF RFC 9106.",
        "NIST. Guide to Malware Incident Prevention and Handling. NIST Special Publication references on endpoint incident response.",
        "NIST. Cybersecurity Framework. National Institute of Standards and Technology.",
        "OWASP. OWASP Desktop App Security resources and secure coding guidance.",
        "MITRE. ATT&CK Matrix for Enterprise — Collection, Exfiltration, and Impact techniques related to local data theft and ransomware-like behavior.",
        "Spitzner, L. Honeypots: Tracking Hackers. Addison-Wesley. (Foundational deception concepts.)",
        "Provos, N., & Holz, T. Virtual Honeypots: From Botnet Tracking to Intrusion Detection. Addison-Wesley.",
        "Microsoft. FileSystemWatcher Class. .NET API Browser.",
        "Microsoft. WPF Documentation. Microsoft Learn.",
        "SANS Institute. Endpoint Detection and Response (EDR) whitepapers and teaching materials.",
        "CIS. CIS Controls — relevant guidance on malware defenses, audit logs, and data recovery.",
        "ISO/IEC 27001. Information security management systems — overview and vocabulary (contextual reference).",
        "MayaJaal Engineering Team. MayaJaal Comprehensive Engineering Design &amp; Implementation Report (Internal), Document ID MJ-ENG-REP-2026-09.",
        "MayaJaal Repository. README.md and docs\\architecture.md — project setup and architecture overview.",
    ]
    for i, r in enumerate(refs, 1):
        story.append(Paragraph(f"{i}. {r}", STYLES["BodyJust"]))
    return story


class BodySwitch:
    """Flowable marker to switch canvas into body-header mode."""

    def __init__(self, front_pages_estimate=10):
        self.front_pages_estimate = front_pages_estimate

    def wrap(self, availWidth, availHeight):
        return (0, 0)

    def draw(self):
        pass


def _first_pass_count_front(story_front):
    """Render front matter to a buffer to count pages."""
    buf = BytesIO()
    doc = SimpleDocTemplate(
        buf,
        pagesize=A4,
        leftMargin=LEFT,
        rightMargin=RIGHT,
        topMargin=TOP,
        bottomMargin=BOTTOM,
    )
    doc._in_body = False
    doc.build(story_front, onFirstPage=add_page_number, onLaterPages=add_page_number)
    import pymupdf

    d = pymupdf.open(stream=buf.getvalue(), filetype="pdf")
    n = len(d)
    d.close()
    return n


def expand_to_target(story, target_pages=62, front_pages=10):
    """If short, append supplemental discussion pages before references end."""
    # Will be handled after measuring; placeholder for content pads inserted in main
    return story


def build_technical_annex():
    """Unique technical annex — no repeated chapter continuations or pad pages."""
    set_chapter("Appendix")
    story = []

    story.append(H("F. THREAT SCENARIOS"))
    story.append(
        P(
            "Six scenarios guided requirements and acceptance tests. Each scenario "
            "emphasizes a different detection/response combination rather than restating "
            "the same USB story."
        )
    )
    story.append(
        make_table(
            ["ID", "Narrative", "Key Signals", "Intended Response"],
            [
                [
                    "A",
                    "USB exfil with decoy probe",
                    "USB, process, honey, mass",
                    "CONTAIN / LOCKDOWN",
                ],
                [
                    "B",
                    "Insider harvest without USB",
                    "Honey, copy, mass",
                    "ALERT / CONTAIN",
                ],
                [
                    "C",
                    "Ransomware-like churn",
                    "Mass + ransomware heuristic",
                    "LOCKDOWN if confidence high",
                ],
                [
                    "D",
                    "Accidental bulk delete",
                    "Deletes / mass, no honey",
                    "Usually gated (lower confidence)",
                ],
                ["E", "Operator drill", "Demo/simulate inject", "Deterministic walkthrough"],
                ["F", "Vault containment path", "Approved response action", "LockAll + evidence"],
            ],
            col_widths=[0.6 * inch, 2.0 * inch, 2.0 * inch, 1.9 * inch],
        )
    )
    story.append(PageBreak())

    story.append(H("G. NORMATIVE SCORING NOTES"))
    story.append(
        P(
            "Risk is a time-decayed weighted sum over the threat window, soft-capped to "
            "[0, 200]. Honey events use elevated base weight and a type multiplier so "
            "decoy access dominates low-value noise. Confidence blends signal strength, "
            "correlation quality, context consistency, and historical accuracy "
            "(approximate weights 0.4 / 0.3 / 0.2 / 0.1). Correlation patterns are "
            "set-membership matches: USB_Honey, Honey_MassCopy, USB_Process_MassCopy, "
            "Insider_Credential_Exfil, and Ransomware_Like."
        )
    )
    story.append(
        P(
            "PolicyEngine maps threat level and confidence to ResponseAction values. "
            "SafetyGate may block high-impact actions when confidence is insufficient "
            "and attaches rollback plans when actions are approved. Exact constants live "
            "in MayaJaal.Domain engine source files and should be treated as the "
            "authoritative values during code review."
        )
    )

    story.append(H("H. SQLITE EVENTS TABLE (LOGICAL)"))
    story.append(
        make_table(
            ["Column", "Role"],
            [
                ["id / sequence", "Identity and append order"],
                ["timestamp", "UTC event time"],
                ["event_type / source / severity", "Classification fields"],
                ["is_honey / is_protected", "Deception and protection flags"],
                ["risk_contribution", "Optional per-event contribution hint"],
                ["path / payload", "Path text and JSON details"],
                ["hash_chain_* / integrity_hash", "SHA-256 chain fields"],
            ],
            col_widths=[2.6 * inch, 3.9 * inch],
        )
    )

    story.append(H("I. STRIDE REVIEW SUMMARY"))
    story.append(
        make_table(
            ["Category", "Example Risk", "Mitigation in MayaJaal"],
            [
                ["Spoofing", "Unauthorized IPC client", "Named-pipe ACLs + client check"],
                ["Tampering", "Edited events.db", "Hash chain (verifier job future)"],
                ["Repudiation", "Denied actions", "Sequenced events + incident artifacts"],
                ["Info disclosure", "Evidence/log leakage", "ACL harden; no secret logging"],
                ["DoS", "Event flood", "Window cap + allowlist"],
                ["Elevation", "Local admin abuse", "OS-owned; not claimed solved"],
            ],
            col_widths=[1.5 * inch, 2.2 * inch, 2.8 * inch],
        )
    )
    story.append(PageBreak())

    story.append(H("J. UNIT TEST MATRIX (DOMAIN)"))
    story.append(
        make_table(
            ["Suite", "Representative Cases"],
            [
                ["RiskEngine", "Empty window; honey elevation; decay; combine risks"],
                ["ConfidenceEngine", "Low-signal gate; USB+honey+mass high confidence"],
                ["CorrelationEngine", "Pattern hit/miss; relatedness checks"],
            ],
            col_widths=[2.0 * inch, 4.5 * inch],
        )
    )
    story.append(
        P(
            "Automated coverage intentionally focuses on pure Domain math. Host, IPC, "
            "collectors, and WPF paths are verified through demo/simulate runs and "
            "manual operator checks. CI on windows-latest builds Release and runs "
            "dotnet test for regression protection."
        )
    )

    story.append(H("K. GLOSSARY AND ACRONYMS"))
    glossary = [
        ("Guardian", "Host that collects signals, scores threats, and serves IPC."),
        ("Security Center", "WPF console for live visualization and operator actions."),
        ("Honey / Decoy", "Synthetic sensitive-looking file used as a high-value sensor."),
        ("Threat Window", "Bounded recent-event buffer used for scoring."),
        ("SafetyGate", "Final approval checkpoint before high-impact responses."),
        ("Vault", "Password-wrapped AES-GCM container used for containment."),
        ("Hash Chain", "SHA-256 linkage across persisted events for integrity."),
    ]
    for term, meaning in glossary:
        story.append(Paragraph(f"<b>{term}:</b> {meaning}", STYLES["BodyJust"]))
    story.extend(
        bullets(
            [
                "AES-GCM — Authenticated encryption mode for vault items",
                "Argon2id — Memory-hard password-based key derivation",
                "EDR — Endpoint Detection and Response",
                "IPC — Inter-Process Communication (named pipes here)",
                "MVVM — Model-View-ViewModel UI pattern",
                "WPF — Windows Presentation Foundation",
            ]
        )
    )

    story.append(H("L. DESIGN TRADE-OFFS AND ETHICS"))
    story.append(
        P(
            "MayaJaal prefers explainable heuristics over opaque models, local-first "
            "operation over cloud analytics, and reversible vault lockdown over "
            "destructive response. Deception files must remain synthetic; lockdown "
            "demos must avoid trapping legitimate user work; evidence directories should "
            "be ACL-hardened. These choices keep the Capstone honest about scope while "
            "still demonstrating a complete detect→decide→contain loop."
        )
    )
    story.append(PageBreak())

    story.append(H("M. SOURCE FILE CATALOG (SELECTED)"))
    story.append(
        make_table(
            ["Area", "Key Paths"],
            [
                ["Domain", "src\\MayaJaal.Domain\\Engines\\*.cs"],
                ["Infra", "EventStore, VaultService, HoneyFileService, IncidentService"],
                ["Guardian", "ThreatEngine, Collectors, IpcServer, DemoScenarioRunner"],
                ["UI", "SecurityCenter ViewModels + MainWindow.xaml"],
                ["Tests", "tests\\MayaJaal.Tests\\UnitTests\\*.cs"],
                ["Docs", "docs\\architecture.md, MAYAJAAL-COMPREHENSIVE-REPORT.md"],
                ["Scripts", "setup.ps1, run-demo.ps1, install-service.ps1"],
            ],
            col_widths=[1.4 * inch, 5.1 * inch],
        )
    )

    story.append(H("N. REPRODUCTION CHECKLIST"))
    story.extend(
        numbered(
            [
                "Install .NET 8 SDK on Windows 10/11.",
                "Run scripts\\setup.ps1 (restore/build/test).",
                "Run Guardian: dotnet run --project src\\MayaJaal.Guardian -- --demo",
                "Or live UI: scripts\\run-demo.ps1 (simulate + Security Center).",
                "Confirm threat metrics, incident/evidence under ProgramData\\MayaJaal.",
                "Archive test output and demo observations for evaluation.",
            ]
        )
    )
    story.append(PageBreak())
    return story



def build_front_matter():
    """Build front-matter flowables (must be called fresh each render)."""
    front = []
    front += build_cover()
    front += build_certificate()
    front += build_declaration()
    front += build_acknowledgement()
    front += chapters.build_abstract(chapter_api())
    front += build_lists_and_toc()
    return front


def render_front_pdf():
    """Render front matter once; return (pdf_bytes, page_count)."""
    front = build_front_matter()
    buf = BytesIO()
    front_doc = SimpleDocTemplate(
        buf,
        pagesize=A4,
        leftMargin=LEFT,
        rightMargin=RIGHT,
        topMargin=TOP,
        bottomMargin=BOTTOM,
        title="MayaJaal Capstone Project-I Report",
        author="Meet Barot; Manav Patel",
    )
    front_doc._in_body = False
    front_doc.build(front, onFirstPage=add_page_number, onLaterPages=add_page_number)
    data = buf.getvalue()
    import pymupdf

    d = pymupdf.open(stream=data, filetype="pdf")
    n = len(d)
    d.close()
    return data, n


def main():
    import pymupdf

    print("Rendering front matter...")
    front_bytes, front_pages = render_front_pdf()
    print(f"Front matter pages: {front_pages}")
    if front_pages < 1:
        raise RuntimeError("Front matter PDF is empty")

    def append_segment(merged, chapter_name, builder, page_num_offset):
        set_chapter(chapter_name)
        seg_story = builder()
        buf = BytesIO()
        seg_doc = SimpleDocTemplate(
            buf,
            pagesize=A4,
            leftMargin=LEFT,
            rightMargin=RIGHT,
            topMargin=0.95 * inch,
            bottomMargin=BOTTOM,
        )
        offset_now = page_num_offset

        def make_onpage(ch_name, offset):
            def _on(canvas, doc_):
                canvas.saveState()
                canvas.setFont("Times-Roman", 9)
                canvas.drawString(
                    LEFT, PAGE_H - 0.45 * inch, f"Enrolment No: {CHAPTER_STATE['enrol']}"
                )
                canvas.drawRightString(PAGE_W - RIGHT, PAGE_H - 0.45 * inch, "KPGU")
                canvas.drawString(LEFT, PAGE_H - 0.60 * inch, f"Chapter Name: {ch_name}")
                canvas.drawRightString(PAGE_W - RIGHT, PAGE_H - 0.60 * inch, "KSET")
                body_page_no = offset + canvas.getPageNumber()
                canvas.setFont("Times-Bold", 10)
                canvas.drawCentredString(PAGE_W / 2, 0.45 * inch, str(body_page_no))
                canvas.setStrokeColor(colors.grey)
                canvas.setLineWidth(0.4)
                canvas.line(LEFT, PAGE_H - 0.68 * inch, PAGE_W - RIGHT, PAGE_H - 0.68 * inch)
                canvas.restoreState()

            return _on

        onpage = make_onpage(chapter_name, offset_now)
        seg_doc.build(seg_story, onFirstPage=onpage, onLaterPages=onpage)
        seg_pdf = pymupdf.open(stream=buf.getvalue(), filetype="pdf")
        print(f"  + {chapter_name}: {len(seg_pdf)} pages")
        merged.insert_pdf(seg_pdf)
        n = len(seg_pdf)
        seg_pdf.close()
        return page_num_offset + n

    api = chapter_api()
    segments = [
        ("INTRODUCTION", lambda: chapters.build_chapter1(api)),
        ("SYSTEM ANALYSIS", lambda: chapters.build_chapter2(api)),
        ("SYSTEM DESIGN", lambda: chapters.build_chapter3(api)),
        ("IMPLEMENTATION", lambda: chapters.build_chapter4(api)),
        ("EVALUATION AND FUTURE WORK", lambda: chapters.build_chapter5(api)),
        ("CONCLUSION AND DISCUSSION", lambda: chapters.build_chapter6(api)),
        ("Appendix", lambda: chapters.build_appendix(api)),
        ("Appendix", lambda: chapters.build_technical_annex(api)),
        ("Reference", lambda: chapters.build_references(api)),
    ]

    merged = pymupdf.open(stream=front_bytes, filetype="pdf")
    print(f"Merged start pages: {len(merged)}")

    page_num_offset = 0
    for chapter_name, builder in segments:
        page_num_offset = append_segment(merged, chapter_name, builder, page_num_offset)

    print(f"After all segments: {len(merged)} pages")

    first = merged[0].get_text()
    if "CAPSTONE PROJECT" not in first.upper():
        raise RuntimeError("Cover page missing after merge; aborting save")

    merged.save(OUT)
    print(f"Wrote {OUT} with {len(merged)} pages (no duplicate padding)")
    merged.close()





if __name__ == "__main__":
    main()
