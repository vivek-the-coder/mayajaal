"""Detailed professional chapter content for MayaJaal Capstone Report."""

from reportlab.lib.units import inch
from reportlab.platypus import PageBreak, Paragraph, Spacer


def _bind(api):
    """Unpack helpers from generator module."""
    return api


def build_abstract(api):
    P, H = api["P"], api["H"]
    story = []
    story.append(P("ABSTRACT", "FrontHeading"))
    story.append(
        P(
            "Endpoint security has evolved beyond signature-based malware detection. "
            "Contemporary threats frequently involve insider data exfiltration, "
            "removable-media copying, credential probing, and ransomware-like file "
            "modification. In such cases, individual telemetry signals may appear weak "
            "or ambiguous when examined in isolation, yet become highly significant "
            "when correlated with deception artifacts and behavioral context. Academic "
            "laboratories and single-workstation environments also face a practical "
            "gap: commercial Endpoint Detection and Response (EDR) platforms are often "
            "costly, cloud-dependent, and opaque in their scoring logic, which limits "
            "their usefulness for teaching, auditing, and controlled demonstration."
        )
    )
    story.append(
        P(
            "MayaJaal – Local-First Cyber Deception and Endpoint Defense Platform "
            "addresses this gap by integrating decoy-based deception, multi-signal "
            "behavioral scoring, policy-driven response selection, safety gating, "
            "cryptographic containment, and operator visualization into a coherent "
            "Windows prototype. The system comprises two cooperating processes. The "
            "Guardian host collects file-system, Universal Serial Bus (USB), and "
            "process signals; maintains a sliding threat window; evaluates risk, "
            "confidence, and named correlation patterns; and executes approved "
            "containment actions. The Security Center presents live threat state to "
            "an operator through a Windows Presentation Foundation (WPF) console "
            "connected over a named-pipe inter-process communication (IPC) channel."
        )
    )
    story.append(
        P(
            "The implemented platform focuses on five functional pillars: (i) deception "
            "and telemetry collection through six honey-file templates and endpoint "
            "collectors; (ii) transparent threat scoring via Risk, Confidence, and "
            "Correlation engines with explicit, unit-tested formulas; (iii) policy and "
            "safety-gated response selection to reduce unsafe automation; (iv) "
            "reversible AES-256-GCM vault lockdown with Argon2id key derivation, "
            "SQLite persistence, SHA-256 hash chaining, and incident evidence "
            "packaging; and (v) operator visualization and control through Security "
            "Center. Supporting operational modes include console hosting, "
            "deterministic demo escalation, continuous simulation, and Windows Service "
            "installation scripts."
        )
    )
    story.append(
        P(
            "Technically, MayaJaal is developed in C# 12 on .NET 8 using Clean "
            "Architecture layering across Shared, Domain, Infrastructure, Guardian, "
            "and Security Center projects. Domain engines remain free of input/output "
            "side effects to enable rigorous unit testing. Infrastructure services "
            "provide persistence, cryptography, decoy deployment, incident management, "
            "and PDF report generation. Continuous integration on GitHub Actions "
            "validates Release builds and domain tests on Windows runners."
        )
    )
    story.append(
        P(
            "The project demonstrates that deception signals and weak behavioral "
            "indicators can be composed into high-confidence defensive decisions when "
            "scoring is transparent, responses are confidence-gated, and containment "
            "is reversible and evidence-backed. The modular design also establishes a "
            "foundation for future enhancements such as deeper telemetry sensors, "
            "continuous integrity verification, richer automated testing, and "
            "professional packaging, without abandoning the local-first and "
            "explainable-security principles that define the Capstone contribution."
        )
    )
    story.append(
        P(
            "<b>Keywords:</b> Cyber Deception, Endpoint Defense, Honey Files, Risk "
            "Scoring, Confidence Engine, Attack Correlation, AES-GCM Vault, Named-Pipe "
            "IPC, Windows Security, Clean Architecture, MayaJaal.",
            "Keyword",
        )
    )
    story.append(PageBreak())
    return story


def build_chapter1(api):
    P, H, bullets, numbered = api["P"], api["H"], api["bullets"], api["numbered"]
    set_chapter = api["set_chapter"]
    set_chapter("INTRODUCTION")
    story = []
    story.append(P("CHAPTER 1: INTRODUCTION", "ChapterTitle"))

    story.append(H("1.1 BACKGROUND"))
    story.append(
        P(
            "Information systems in educational institutions, research laboratories, and "
            "small organizations increasingly depend on Windows workstations as the "
            "primary environment for creating, storing, and exchanging sensitive "
            "documents. These endpoints commonly contain financial worksheets, "
            "credential notes, strategic plans, personal records, and application "
            "secrets. As a result, the workstation has become a high-value target for "
            "both external malware operators and trusted insiders who already possess "
            "legitimate local access."
        )
    )
    story.append(
        P(
            "Historically, endpoint defense emphasized antivirus signatures and "
            "perimeter controls. While these measures remain necessary, they are "
            "insufficient against many modern abuse patterns. An insider may insert a "
            "USB drive and copy large portions of a Documents folder without dropping "
            "a malicious binary. A credential-harvesting tool may touch only a few "
            "attractive files. Ransomware-like activity may appear first as rapid "
            "rename and modify bursts rather than as a previously known executable. "
            "In each case, waiting for a definitive malware label can allow irreversible "
            "data loss or disclosure."
        )
    )
    story.append(
        P(
            "Commercial EDR products attempt to close this gap through broad telemetry "
            "and analytics, often backed by cloud services. Such platforms are powerful "
            "in enterprise settings, yet they introduce constraints for Capstone and "
            "laboratory contexts: licensing cost, dependency on external connectivity, "
            "and limited visibility into the exact mathematical basis of alerts. For "
            "students of Computer Science and Engineering, an opaque cloud score is "
            "pedagogically weaker than a system whose risk and confidence calculations "
            "can be inspected, tested, and explained during a viva voce examination."
        )
    )
    story.append(
        P(
            "Deception technology offers a complementary principle. Instead of only "
            "watching real assets, the defender plants attractive but synthetic assets—"
            "honey files or decoys—where unauthorized interest becomes an immediate "
            "high-value signal. A decoy that resembles a password store, payroll "
            "export, or strategic memorandum is unlikely to be opened during ordinary "
            "work. When such a file is accessed in combination with USB activity or "
            "mass copying, the defensive narrative becomes substantially stronger than "
            "any one signal alone."
        )
    )
    story.append(
        P(
            "MayaJaal, a name suggesting an “illusion” or “web of deception,” is "
            "conceived as a local-first Windows cyber-defense prototype that unifies "
            "these ideas. It deploys multiple decoy templates, collects endpoint "
            "telemetry, scores a sliding threat window with explicit engines, "
            "correlates named attack patterns, and can contain impact through "
            "reversible cryptographic vault lockdown while retaining durable evidence. "
            "A dedicated Security Center console makes the resulting threat state "
            "visible to an operator without requiring a cloud control plane."
        )
    )
    story.append(
        P(
            "From an academic perspective, the project integrates several core areas "
            "of the undergraduate curriculum: software architecture, operating-system "
            "services, cryptography, database persistence, human–computer interaction "
            "for security operators, and threat-informed design. The Capstone therefore "
            "is not merely an assembly of application programming interfaces; it is a "
            "complete detect–decide–contain narrative implemented as working software."
        )
    )

    story.append(H("1.2 PROBLEM STATEMENT"))
    story.append(
        P(
            "The central problem addressed by MayaJaal is decision-making under "
            "uncertainty on a local Windows endpoint. Defenders must determine when "
            "ordinary-looking activity should escalate into alert or containment. A "
            "USB insertion may be legitimate. A burst of file operations may be a "
            "student rearranging coursework. A process start may be a new development "
            "tool. Yet combinations of these events with honey-file access can indicate "
            "active exfiltration or destructive intent."
        )
    )
    story.append(
        P(
            "Current practice in many small environments leaves this decision either "
            "to human vigilance—which does not scale—or to commercial agents whose "
            "internals cannot be audited in coursework. Specific weaknesses include:"
        )
    )
    story.extend(
        numbered(
            [
                "Fragmentation of local visibility across antivirus, device logs, and ad-hoc scripts, with little correlation among signals.",
                "Limited use of deception artifacts as first-class sensors inside a scoring pipeline.",
                "Absence of explicit confidence thresholds before high-impact automated actions.",
                "Dependence on cloud analytics that may be unavailable in offline laboratories.",
                "Weak local evidence integrity, making later review of incidents difficult.",
                "Insufficient pedagogical transparency for students who must justify why a system escalated.",
            ]
        )
    )
    story.append(
        P(
            "Accordingly, there is a need for an integrated local platform that can "
            "deploy and monitor honey files; collect file, USB, and process telemetry "
            "into a common event model; compute risk with time decay and honey-aware "
            "weighting; estimate multi-factor confidence; recognize named multi-signal "
            "patterns; select responses through policy; apply a safety gate before "
            "disruptive actions; persist events with integrity-friendly storage; and "
            "present the outcome through an operator console. MayaJaal is designed "
            "expressly to satisfy this combined requirement set within Capstone scope."
        )
    )

    story.append(H("1.3 OBJECTIVES"))
    story.append(
        P(
            "The primary objective of this Capstone Project is to design, implement, "
            "and evaluate a local-first cyber deception and endpoint defense platform "
            "that demonstrates transparent scoring and controlled containment on "
            "Windows. The specific objectives are as follows:"
        )
    )
    story.extend(
        numbered(
            [
                "To develop a Guardian host capable of collecting endpoint security signals and executing a continuous threat-evaluation pipeline.",
                "To implement honey-file deception with multiple decoy templates representing sensitive document categories.",
                "To design and implement Risk, Confidence, and Correlation engines with explicit formulas and automated unit tests.",
                "To implement PolicyEngine and SafetyGate components that map threat state to response actions under confidence constraints.",
                "To provide AES-256-GCM vault lockdown with Argon2id-derived wrapping keys for reversible containment.",
                "To persist security events in SQLite using sequence numbers and SHA-256 hash chaining, and to generate incident evidence artifacts.",
                "To develop a Security Center WPF console that visualizes threat metrics, events, incidents, vaults, and policies over named-pipe IPC.",
                "To support demo, simulate, console, and Windows Service operational modes suitable for teaching, verification, and local deployment.",
                "To document architecture, algorithms, limitations, and evaluation results in a professional Capstone report.",
            ]
        )
    )

    story.append(H("1.4 SCOPE"))
    story.append(
        P(
            "The scope of Capstone Project – I covers the complete design and "
            "implementation of MayaJaal as a modular Windows prototype. The following "
            "subsections define the major functional areas included in the work."
        )
    )

    story.append(H("1.4.1 Deception and Telemetry Collection", "SubSectionHead"))
    story.append(
        P(
            "This area includes deployment of six honey decoy templates under "
            "Documents\\MayaJaal-Decoys; FileSystemWatcher-based monitoring of "
            "Documents and decoy directories; synthesis of mass-activity events when "
            "operation volume exceeds configured thresholds; USB insert/remove "
            "collection; and process-start collection for suspicious process names. "
            "A process allowlist reduces noise from trusted system processes, while "
            "honey-related events always bypass suppression so deception signals are "
            "never discarded as benign noise."
        )
    )

    story.append(H("1.4.2 Threat Scoring Engines", "SubSectionHead"))
    story.append(
        P(
            "This area covers the RiskEngine, ConfidenceEngine, and CorrelationEngine "
            "operating over a sliding in-memory threat window of approximately fifteen "
            "minutes and at most five hundred events. Risk is computed as a "
            "time-decayed weighted sum with a soft cap. Confidence combines signal "
            "strength, correlation quality, context consistency, and historical "
            "accuracy. Correlation detects named set-membership patterns such as "
            "USB_Honey and USB_Process_MassCopy. These engines are implemented as "
            "pure Domain components to support deterministic testing."
        )
    )

    story.append(H("1.4.3 Policy and Safety-Gated Response", "SubSectionHead"))
    story.append(
        P(
            "This area includes mapping threat level and confidence to response actions "
            "such as monitor, alert, contain, and emergency lockdown, and applying "
            "SafetyGate checks that may approve or block high-impact actions. Incident "
            "creation, status updates, resolve operations, and rollback-oriented "
            "command paths are included so automated defense remains reviewable and, "
            "where appropriate, reversible."
        )
    )

    story.append(H("1.4.4 Cryptographic Vault and Evidence", "SubSectionHead"))
    story.append(
        P(
            "This area covers vault create, unlock, and lock flows; AES-GCM item "
            "encryption; Argon2id key derivation; DPAPI-protected bootstrap secrets for "
            "local demo convenience; SQLite event persistence with hash chaining; "
            "incident evidence JSON; and QuestPDF incident report generation. The "
            "design intentionally prefers reversible containment over destructive "
            "wiping of user data."
        )
    )

    story.append(H("1.4.5 Security Center Operator Console", "SubSectionHead"))
    story.append(
        P(
            "This area covers the WPF MVVM Security Center that polls Guardian "
            "approximately every two seconds for status, events, incidents, vaults, "
            "and policies; displays an offline banner when IPC is unavailable; and "
            "supports selected operator actions such as manual vault lock where exposed "
            "by the IPC contract. The console is treated as a thin client of Shared "
            "contracts rather than a direct consumer of Domain or Infrastructure types."
        )
    )
    story.append(
        P(
            "Explicitly outside Capstone scope are kernel minifilters, full packet "
            "capture, multi-tenant cloud fleet management, claims of commercial EDR "
            "replacement, and formal Common Criteria or FIPS certification. These "
            "boundaries are stated to keep evaluation criteria honest and achievable."
        )
    )

    story.append(H("1.5 ORGANIZATION OF REPORT"))
    story.append(
        P(
            "The remainder of this report is organized to move from problem framing "
            "to analysis, design, implementation, evaluation, and conclusion."
        )
    )
    story.extend(
        bullets(
            [
                "Chapter 2 presents system analysis, including study of current systems, weaknesses, requirements, feasibility, activities, modules, and software/hardware needs.",
                "Chapter 3 describes system design, architecture, collectors, engines, vault and evidence design, interfaces, and security controls.",
                "Chapter 4 details implementation, technology stack, module development, testing, demo narrative, and implementation challenges.",
                "Chapter 5 evaluates results, discusses strengths and limitations, and outlines future enhancements.",
                "Chapter 6 concludes the work and discusses design trade-offs and academic significance.",
                "The Appendix provides normative tables, worked examples, catalogs, and reproduction guidance. References list cited sources.",
            ]
        )
    )
    story.append(PageBreak())
    return story


def build_chapter2(api):
    P, H, bullets, numbered, make_table = (
        api["P"],
        api["H"],
        api["bullets"],
        api["numbered"],
        api["make_table"],
    )
    set_chapter = api["set_chapter"]
    set_chapter("SYSTEM ANALYSIS")
    story = []
    story.append(P("CHAPTER 2: SYSTEM ANALYSIS", "ChapterTitle"))

    story.append(H("2.1 STUDY OF CURRENT SYSTEM"))
    story.append(
        P(
            "Workstation security in contemporary practice is typically assembled from "
            "several independent layers. Antivirus software scans files and processes "
            "against known indicators. Operating-system protections such as User Account "
            "Control, Windows Defender features, and removable-storage policies provide "
            "baseline hardening. In larger organizations, EDR agents stream telemetry "
            "to a centralized analytics plane. Separately, administrators may inspect "
            "Event Viewer logs, enable auditing policies, or deploy Data Loss Prevention "
            "(DLP) rules that classify content leaving the endpoint."
        )
    )
    story.append(
        P(
            "In academic laboratories, the situation is often simpler and more "
            "fragmented. Students and faculty may rely on built-in antivirus, occasional "
            "manual checks, and course-specific tools. Network honeypots, where present, "
            "observe external scanning rather than local document theft. Sample programs "
            "used in operating-system or security courses demonstrate individual APIs—"
            "for example file watchers or encryption routines—but rarely compose them "
            "into an end-to-end defensive workflow with incidents, evidence, and an "
            "operator console."
        )
    )
    story.append(
        P(
            "A careful study of this landscape reveals both capabilities and gaps. "
            "Antivirus remains effective against many commodity threats. EDR is "
            "effective in enterprises with budget and operations staff. DLP helps when "
            "content classification is mature. None of these, however, simultaneously "
            "satisfy the Capstone needs of local operation, deception-centered "
            "correlation, explainable scoring, reversible containment, and classroom "
            "demonstrability. MayaJaal is therefore positioned not as a replacement for "
            "all existing tools, but as an integrated local prototype that occupies the "
            "pedagogical and architectural space between toy samples and opaque "
            "commercial suites."
        )
    )
    story.append(P("Figure 2.1: High-Level Use Context of MayaJaal", "Caption"))
    story.append(
        P(
            "Figure 2.1 (conceptual) places the workstation user, potential insider or "
            "malware process, removable media, operating-system file APIs, optional "
            "antivirus/EDR, Guardian, Security Center, and the human operator in one "
            "context. MayaJaal inserts decoys and a local scoring host into this "
            "environment and presents results through the operator console."
        )
    )

    story.append(H("2.2 PROBLEMS AND WEAKNESSES OF CURRENT SYSTEM"))
    story.append(
        P(
            "Analysis of existing approaches identifies the following principal "
            "weaknesses relevant to the Capstone problem:"
        )
    )
    story.append(H("1. Fragmented Local Visibility", "SubSectionHead"))
    story.append(
        P(
            "File activity, USB events, and process starts are frequently observed by "
            "different tools, if they are observed at all. Without a shared event model, "
            "correlation becomes an informal mental exercise performed by an "
            "administrator after damage has occurred."
        )
    )
    story.append(H("2. Weak Integration of Deception", "SubSectionHead"))
    story.append(
        P(
            "Honey files, when used, may generate isolated alerts. They are rarely "
            "combined systematically with mass-copy detection and process context inside "
            "one local pipeline that can also act on the result."
        )
    )
    story.append(H("3. Opaque Scoring and Limited Auditability", "SubSectionHead"))
    story.append(
        P(
            "Commercial engines seldom expose normative formulas suitable for academic "
            "audit, unit testing, or classroom explanation. Students cannot easily "
            "verify why a score crossed a threshold."
        )
    )
    story.append(H("4. Unsafe or Absent Automation", "SubSectionHead"))
    story.append(
        P(
            "Systems that automate containment without confidence gates risk disrupting "
            "legitimate work. Systems that only alert may leave sensitive data exposed "
            "for too long. A balanced design needs both decisive action and safety "
            "checks."
        )
    )
    story.append(H("5. Fragile Evidence Trails", "SubSectionHead"))
    story.append(
        P(
            "Local logs can be incomplete, rotated away, or altered. Without "
            "append-friendly integrity mechanisms and incident packaging, post-incident "
            "review is weak—especially in teaching environments where reproducibility "
            "matters."
        )
    )
    story.append(H("6. Cloud Dependence for Teaching Labs", "SubSectionHead"))
    story.append(
        P(
            "Many modern platforms assume continuous cloud connectivity. Offline "
            "laboratories, controlled networks, and viva demonstrations benefit from a "
            "local-first architecture that can run without external services."
        )
    )

    story.append(H("2.3 REQUIREMENTS OF THE NEW SYSTEM"))
    story.append(H("2.3.1 Functional Requirements", "SubSectionHead"))
    story.append(P("The proposed system shall satisfy the following functional requirements:"))
    story.extend(
        numbered(
            [
                "Deploy a configurable set of honey decoy files representing sensitive document categories under the user Documents tree.",
                "Monitor file-system activity in Documents and decoy directories and map relevant operations to typed security events, distinguishing honey events from ordinary file events.",
                "Detect burst or mass file activity within a short time window and emit a dedicated mass-activity event with de-duplication.",
                "Collect USB insertion and removal events and represent them in the common security-event model.",
                "Collect suspicious process-start events while allowing trusted processes to be skipped, except that honey events must never be suppressed by allowlisting.",
                "Maintain a sliding threat window and compute risk, confidence, and correlation pattern matches after each relevant event.",
                "Select response actions through a policy engine and apply safety-gate checks before high-impact execution.",
                "Create and update incidents, execute approved containment including vault lockdown, and support resolve and rollback-oriented command paths.",
                "Persist events to SQLite with sequence numbers and hash-chain fields, and write incident evidence artifacts suitable for later review.",
                "Expose status and control operations to Security Center via length-prefixed JSON over a named pipe.",
                "Provide a WPF dashboard for threat metrics, timelines/events, incidents, vaults, and policies, including clear offline indication.",
                "Support console, simulate, demo, and Windows Service hosting modes with scripted setup and installation aids.",
            ]
        )
    )

    story.append(H("2.3.2 Non-Functional Requirements", "SubSectionHead"))
    story.append(
        P(
            "In addition to functional behavior, the system must meet quality attributes "
            "appropriate for a Capstone-grade security prototype:"
        )
    )
    story.extend(
        bullets(
            [
                "Reliability: Guardian should continue collecting and scoring under ordinary desktop workloads; Security Center should degrade gracefully when Guardian is offline.",
                "Performance: Scoring and two-second UI polling must remain responsive on typical student or developer laptops; the threat window must remain bounded.",
                "Security: Vault secrets must use Argon2id and AES-GCM; pipe access must be restricted by ACLs; ProgramData permissions should be hardenable by script; secrets must not appear in logs.",
                "Maintainability: Clean Architecture separation must keep Domain engines free of I/O; Shared contracts must isolate IPC shapes from UI internals.",
                "Testability: Domain engines must be unit-testable without UI or Windows Service dependencies.",
                "Usability: Operators must see threat level, risk, confidence, active pattern, and offline state without navigating opaque menus.",
                "Auditability: Formulas, enums, and schemas must be documented so reviewers can reproduce scoring decisions.",
                "Portability within Windows: The solution must target Windows 10/11 with .NET 8.",
            ]
        )
    )
    story.append(PageBreak())

    story.append(H("2.4 SYSTEM FEASIBILITY"))
    story.append(H("2.4.1 Technical Feasibility", "SubSectionHead"))
    story.append(
        P(
            "The project is technically feasible using mainstream Microsoft and "
            "open-source components. .NET 8 provides Windows Service hosting, WPF, "
            "named pipes, and asynchronous programming suitable for collectors and IPC. "
            "FileSystemWatcher, drive polling, and process enumeration are available "
            "through supported APIs. AES-GCM is available in the platform cryptography "
            "stack; Argon2id is available through a maintained NuGet package. SQLite "
            "via Microsoft.Data.Sqlite offers zero-administration local persistence. "
            "QuestPDF can generate local incident reports. xUnit supports automated "
            "testing of pure Domain logic. Collectively, these building blocks are mature "
            "enough for a Capstone timeline while still leaving meaningful engineering "
            "challenges in composition, safety, and operator experience."
        )
    )
    story.append(H("2.4.2 Economic Feasibility", "SubSectionHead"))
    story.append(
        P(
            "Economic feasibility is high for an academic setting. The .NET SDK, "
            "xUnit, Serilog, Microsoft.Data.Sqlite, CommunityToolkit.Mvvm, and "
            "QuestPDF Community licensing for eligible users avoid mandatory cloud "
            "subscriptions for core operation. Development hardware is a standard "
            "Windows PC already available to students. No specialized appliances are "
            "required for the baseline demo, although an optional USB drive improves "
            "live removable-media demonstrations. Maintenance cost is primarily "
            "engineering time rather than recurring license fees."
        )
    )
    story.append(H("2.4.3 Operational Feasibility", "SubSectionHead"))
    story.append(
        P(
            "Operationally, MayaJaal can be exercised in console mode during "
            "development, in simulate or demo modes during presentations, and in "
            "service-install mode for a more production-like local deployment. "
            "Operators interact through Security Center without needing to understand "
            "engine internals. Because the platform is local-first, demonstrations can "
            "proceed offline. Operational caution remains necessary: decoys must stay "
            "synthetic, and automated lockdown must be demonstrated carefully so "
            "legitimate users are not disrupted. SafetyGate and confidence thresholds "
            "exist specifically to support this operational concern."
        )
    )

    story.append(H("2.5 ACTIVITIES IN PROPOSED SYSTEM"))
    story.append(
        P(
            "The proposed system supports a complete activity chain from setup through "
            "incident review:"
        )
    )
    story.extend(
        bullets(
            [
                "Environment setup and build using PowerShell setup scripts and the .NET SDK.",
                "Guardian startup in console or Windows Service form, with optional simulation or deterministic demo scenarios.",
                "Automatic or bootstrap vault provisioning so containment capacity exists before an incident.",
                "Honey decoy deployment and continuous file-system monitoring.",
                "Ingestion of USB and process signals into the common event model.",
                "Continuous risk, confidence, and correlation evaluation over the threat window.",
                "Incident opening, containment execution, evidence packaging, and PDF reporting.",
                "Operator observation and selected manual lock, resolve, or rollback actions via Security Center.",
                "Unit-test verification of Domain engines and CI build validation on Windows runners.",
            ]
        )
    )

    story.append(H("2.6 MAIN MODULES"))
    story.append(
        P(
            "MayaJaal is decomposed into modules that mirror Clean Architecture "
            "boundaries. Table 2.1 summarizes module responsibilities."
        )
    )
    story.append(P("Table 2.1: System Module Descriptions", "Caption"))
    story.append(
        make_table(
            ["Module", "Description"],
            [
                [
                    "Shared Models and IPC",
                    "Defines SecurityEvent, Incident, ThreatState, vault/asset models, and GuardianClient / IPC message contracts used by both processes.",
                ],
                [
                    "Domain Engines",
                    "Pure Risk, Confidence, Correlation, Policy, and SafetyGate logic without file, network, or UI side effects.",
                ],
                [
                    "Infrastructure Services",
                    "SQLite EventStore, cryptography and key derivation, VaultService, HoneyFileService, IncidentService, and ReportGenerator.",
                ],
                [
                    "Guardian Host",
                    "Collectors, ThreatEngine orchestration, named-pipe IpcServer, demo/simulate modes, bootstrap, and service hosting.",
                ],
                [
                    "Security Center",
                    "WPF MVVM operator console with live polling, offline indication, and selected control actions.",
                ],
                [
                    "Tests and Scripts",
                    "xUnit Domain tests; setup, demo, install, uninstall, and ACL hardening scripts; CI workflow.",
                ],
            ],
            col_widths=[2.0 * inch, 4.5 * inch],
        )
    )
    story.append(PageBreak())

    story.append(H("2.7 SOFTWARE AND HARDWARE"))
    story.append(H("2.7.1 Software Requirements", "SubSectionHead"))
    story.extend(
        bullets(
            [
                "Windows 10 or Windows 11 operating system",
                ".NET 8 SDK and runtime, including Windows desktop workload for WPF",
                "Visual Studio 2022 or a C#-capable editor such as VS Code / Cursor (recommended)",
                "PowerShell 5 or later for setup and service scripts",
                "Git for source control",
                "Optional: GitHub Actions access for CI on windows-latest runners",
            ]
        )
    )
    story.append(H("2.7.2 Hardware Requirements", "SubSectionHead"))
    story.append(P("Table 2.2: Hardware Requirements", "Caption"))
    story.append(
        make_table(
            ["Component", "Minimum Recommendation"],
            [
                ["Processor", "64-bit dual-core CPU or better"],
                ["Memory", "8 GB RAM (16 GB preferred when IDE and both processes run together)"],
                ["Storage", "At least 1 GB free for build outputs and ProgramData artifacts"],
                ["Display", "1366×768 or higher for comfortable Security Center use"],
                ["USB", "Optional removable drive for live USB demonstration scenarios"],
            ],
            col_widths=[2.2 * inch, 4.3 * inch],
        )
    )

    story.append(H("2.8 PROPOSED SYSTEM OVERVIEW"))
    story.append(
        P(
            "The proposed MayaJaal system consists of a Guardian process and a Security "
            "Center process collaborating over a local named pipe. Guardian is the "
            "stateful defensive host: it collects signals, stores events, scores threat "
            "state, opens incidents, and serves IPC. Security Center is the operator "
            "face of the system: it polls status and presents risk, confidence, events, "
            "incidents, vaults, and policies."
        )
    )
    story.append(
        P(
            "At runtime, collectors emit SecurityEvent objects into ThreatEngine. The "
            "engine updates the sliding window, may synthesize ransomware-behavior "
            "heuristics, computes risk and confidence, evaluates correlation patterns, "
            "requests a ResponseAction from PolicyEngine, and consults SafetyGate before "
            "executing containment. Approved actions may lock vaults and write evidence "
            "under %ProgramData%\\MayaJaal. The operator observes outcomes in Security "
            "Center within a short polling interval. This overview establishes the "
            "analytical foundation for the detailed design in Chapter 3 and the "
            "implementation discussion in Chapter 4."
        )
    )

    story.append(H("2.9 ASSUMPTIONS AND CONSTRAINTS"))
    story.extend(
        bullets(
            [
                "The endpoint runs Windows 10/11 with a standard user profile containing a Documents folder.",
                "For demos, Guardian and Security Center typically run under the same interactive user context.",
                "Modeled adversaries are primarily user-mode actors or insiders rather than kernel-level attackers.",
                "Network exfiltration beyond local USB and file activity is not the primary sensing focus.",
                "Decoy content is synthetic and must not contain real secrets.",
                "A privileged local administrator can still interfere with user-mode agents; MayaJaal does not claim kernel integrity.",
            ]
        )
    )
    story.append(PageBreak())
    return story


def build_chapter3(api):
    P, H, bullets, numbered, make_table = (
        api["P"],
        api["H"],
        api["bullets"],
        api["numbered"],
        api["make_table"],
    )
    set_chapter = api["set_chapter"]
    set_chapter("SYSTEM DESIGN")
    story = []
    story.append(P("CHAPTER 3: SYSTEM DESIGN", "ChapterTitle"))

    story.append(H("3.1 DESIGN METHODOLOGY"))
    story.append(
        P(
            "MayaJaal follows an iterative, modular methodology inspired by Clean "
            "Architecture. The essential rule is that Domain scoring and policy logic "
            "must not depend on files, pipes, databases, or user-interface frameworks. "
            "Infrastructure adapts technical capabilities to Domain needs. Guardian "
            "hosts the runtime composition. Security Center consumes only Shared "
            "contracts. This methodology enables unit testing of formulas, replacement "
            "of storage or crypto libraries, and independent evolution of the operator "
            "console."
        )
    )
    story.append(
        P(
            "Development proceeded in deliberate increments: first the security-event "
            "model and engine formulas; then Guardian collectors and ThreatEngine "
            "orchestration; then persistence, vault, and incident services; then "
            "Security Center and operational scripts. Demo and simulate modes were "
            "introduced early so scoring behavior could be demonstrated "
            "deterministically during development and evaluation."
        )
    )

    story.append(H("3.2 SYSTEM ARCHITECTURE"))
    story.append(
        P(
            "The logical architecture comprises five primary projects plus tests. "
            "Table 3.1 summarizes layer responsibilities."
        )
    )
    story.append(P("Table 3.1: System Layer Responsibilities", "Caption"))
    story.append(
        make_table(
            ["Layer / Project", "Responsibility"],
            [
                ["Presentation — Security Center", "WPF views/viewmodels; operator actions; IPC client only"],
                ["Host — Guardian", "Collectors, ThreatEngine, IpcServer, bootstrap, service hosting"],
                ["Domain — MayaJaal.Domain", "Pure threat math and policy/safety decisions"],
                ["Infrastructure", "Persistence, cryptography, decoys, incidents, reporting"],
                ["Shared — MayaJaal.Shared", "Cross-process contracts, models, GuardianClient"],
                ["Tests — MayaJaal.Tests", "xUnit verification of Domain engines"],
            ],
            col_widths=[2.3 * inch, 4.2 * inch],
        )
    )
    story.append(P("Figure 3.1: MayaJaal System Architecture", "Caption"))
    story.append(
        P(
            "Runtime data flows from collectors into ThreatEngine, then to EventStore "
            "and Incident/Vault services. IpcServer exposes a length-prefixed UTF-8 "
            "JSON protocol on the named pipe MayaJaal.Guardian. Security Center’s "
            "GuardianStatusService polls this pipe approximately every two seconds. "
            "The architecture deliberately prevents the UI from calling Domain or "
            "Infrastructure types directly, preserving a clear trust and dependency "
            "boundary."
        )
    )

    story.append(H("3.3 SYSTEM MODULE DESIGN"))
    story.append(
        P(
            "Modules interact through interfaces and message contracts. Collectors "
            "depend on IThreatEngine. ThreatEngine depends on Domain engines and "
            "Infrastructure services registered by dependency injection. Security "
            "Center depends only on Shared IPC types. This reduces accidental "
            "complexity and clarifies where validation, persistence, and presentation "
            "concerns belong."
        )
    )
    story.append(
        P(
            "Dependency injection is centralized so that engine implementations can be "
            "substituted in tests and so that Guardian startup remains declarative. "
            "Application-layer helpers forward registration where needed, while "
            "Guardian remains the composition root for host-specific services such as "
            "collectors and the IPC server."
        )
    )

    story.append(H("3.4 DECEPTION AND COLLECTOR DESIGN"))
    story.append(
        P(
            "HoneyFileService deploys six templates—Financial, Credential, Strategy, "
            "HR, API, and Personal—under the user’s Documents\\MayaJaal-Decoys "
            "directory. Templates are synthetic and must never embed real secrets. "
            "Their purpose is attractiveness and detectability, not authenticity of "
            "confidential content."
        )
    )
    story.append(
        P(
            "EventCollector uses FileSystemWatcher callbacks on Documents and decoy "
            "directories, mapping operations to FILE_* or HONEY_* event types. Burst "
            "detection synthesizes MASS_FILE_ACTIVITY when operation volume exceeds a "
            "threshold of approximately twenty-five operations in ten seconds, with "
            "short de-duplication to avoid event storms. UsbCollector periodically "
            "polls removable drives and emits USB_INSERT / USB_REMOVE. ProcessCollector "
            "polls running processes and emits PROCESS_START for suspicious names, "
            "subject to ProcessAllowlist filtering. Honey-related events are never "
            "suppressed by the allowlist."
        )
    )

    story.append(H("3.5 THREAT ENGINE DESIGN"))
    story.append(P("Figure 3.2: Threat Processing Pipeline", "Caption"))
    story.append(
        P(
            "ThreatEngine is the orchestration heart of Guardian. For each incoming "
            "SecurityEvent it typically: (1) optionally drops allowlisted noise; "
            "(2) persists via EventStore with hash chaining; (3) updates the sliding "
            "window; (4) may emit ransomware-behavior heuristic events; (5) computes "
            "risk; (6) computes confidence; (7) evaluates correlation patterns and may "
            "boost risk; (8) asks PolicyEngine for a ResponseAction; (9) asks SafetyGate "
            "for approval when the action is non-trivial; and (10) creates or updates "
            "incidents and executes approved responses. Startup may replay recent "
            "SQLite events into the window to reduce cold-start blindness after restart."
        )
    )
    story.append(
        P(
            "The in-memory window is bounded—approximately fifteen minutes and at most "
            "five hundred events—so memory use and scoring cost remain predictable. "
            "ThreatState exposed over IPC includes current risk, confidence, level, "
            "recent events, and active incident information for the operator console."
        )
    )
    story.append(PageBreak())

    story.append(H("3.6 RISK, CONFIDENCE, AND CORRELATION DESIGN"))
    story.append(
        P(
            "RiskEngine computes a time-decayed weighted sum over recent events and "
            "soft-caps the result to the integer interval [0, 200]. Each event type has "
            "a minimum and maximum base contribution and a decay constant λ per minute. "
            "Honey events receive elevated weights and a type multiplier of 1.5, with a "
            "base-weight floor of 55, so decoy interaction strongly influences the score. "
            "Additional multipliers apply for suspicious processes, USB devices, and "
            "protected assets."
        )
    )
    story.append(
        P(
            "Threat banding maps risk and confidence into SAFE, LOW, MEDIUM, HIGH, and "
            "CRITICAL. When confidence is below approximately 0.35 and raw risk is high, "
            "effective risk used for banding is capped near 50 so uncertain stories do "
            "not present as CRITICAL. This design encodes an important security-"
            "operations principle: escalation should reflect both severity and trust in "
            "the evidence."
        )
    )
    story.append(
        P(
            "ConfidenceEngine combines factors approximately as 0.4·SignalStrength + "
            "0.3·CorrelationQuality + 0.2·ContextConsistency + 0.1·HistoricalAccuracy. "
            "CorrelationQuality rises as USB, honey, mass, and suspicious process "
            "evidence co-occur. ContextConsistency is higher when events arrive within "
            "a short time span. HistoricalAccuracy is bootstrapped to a stable prior "
            "suitable for a prototype without long production history."
        )
    )
    story.append(
        P(
            "CorrelationEngine evaluates named set-membership patterns, including "
            "USB_Honey, Honey_MassCopy, USB_Process_MassCopy, "
            "Insider_Credential_Exfil, and Ransomware_Like. Patterns are intentionally "
            "named for teaching and viva explanation. If an event is marked IsHoney but "
            "typed as an ordinary FILE_* operation, the engine normalizes it to "
            "HONEY_ACCESS for matching so decoy touches are not lost."
        )
    )

    story.append(H("3.7 POLICY AND SAFETY GATE DESIGN"))
    story.append(
        P(
            "PolicyEngine maps threat level and confidence to ResponseAction values "
            "such as NONE/MONITOR, ALERT, CONTAIN, and EMERGENCY_LOCKDOWN. For "
            "example, CRITICAL combined with confidence at or above 0.85 is associated "
            "with emergency lockdown. Lower bands produce milder actions. The policy "
            "layer answers “what should we do if we trust the story?”"
        )
    )
    story.append(
        P(
            "SafetyGate is the last defensive checkpoint before high-impact execution. "
            "It may block lockdown when confidence is insufficient and associates "
            "rollback plans with approved actions. Separating policy desire from safety "
            "approval prevents a single component from both proposing and irrevocably "
            "executing disruptive behavior. Threshold values across ConfidenceEngine, "
            "PolicyEngine, and SafetyGate are related but not always identical; "
            "auditors should read Domain source as the authority during review."
        )
    )

    story.append(H("3.8 VAULT AND CRYPTOGRAPHY DESIGN"))
    story.append(
        P(
            "VaultService manages password-wrapped vaults under "
            "ProgramData\\MayaJaal\\Vaults. A password and salt derive a wrapping key "
            "via Argon2id with design defaults such as memory cost 65536, iterations 3, "
            "and parallelism 1. A random 32-byte master key is AES-GCM-wrapped into "
            "vault metadata. Item payloads are stored as encrypted blobs with a "
            "12-byte IV size consistent with AES-GCM practice."
        )
    )
    story.append(
        P(
            "DPAPI protects a default vault bootstrap secret so Guardian can "
            "auto-provision and unlock a containment vault on local machine scope for "
            "demos. LockAll encrypts or locks unlocked vaults during emergency "
            "response, providing reversible containment rather than destructive wiping. "
            "This choice aligns with Capstone ethics: demonstrate decisive response "
            "without permanently destroying student or operator data."
        )
    )

    story.append(H("3.9 DATABASE AND EVIDENCE DESIGN"))
    story.append(P("Figure 3.3: Event Store and Vault Data Layout", "Caption"))
    story.append(
        P(
            "EventStore uses Microsoft.Data.Sqlite with an events table containing "
            "identifiers, sequence, timestamp, event type, source, severity, honey and "
            "protected flags, risk contribution, path, full JSON payload, and "
            "hash-chain fields. Each insert computes a SHA-256 integrity hash over a "
            "compact canonical field set chained to the previous hash or GENESIS."
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
                ["Evidence pack", "JSON and PDF artifacts under ProgramData\\Evidence"],
            ],
            col_widths=[2.0 * inch, 4.5 * inch],
        )
    )
    story.append(
        P(
            "Incidents are numbered INC-{yyyyMMdd}-{counter:D4}. ReportGenerator can "
            "render incident PDFs using QuestPDF for operator and archival use. "
            "Evidence is attached at containment time so later resolution does not erase "
            "the technical trail required for evaluation and after-action review."
        )
    )
    story.append(PageBreak())

    story.append(H("3.10 INTERFACE DESIGN"))
    story.append(H("3.10.1 Named-Pipe IPC Interface", "SubSectionHead"))
    story.append(
        P(
            "IPC frames are length-prefixed UTF-8 JSON: a 32-bit little-endian length "
            "followed by the payload. This framing avoids ambiguity about message "
            "boundaries on a byte stream. Table 3.3 lists principal commands."
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
                ["GET_VAULTS", "Vault inventory and state"],
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
            "Security Center provides a dashboard with threat level, risk score, "
            "confidence, correlation pattern, summary indicators, and an event "
            "timeline. Navigation pages cover incidents, vaults, and policies. An "
            "offline banner appears when Guardian cannot be reached, ensuring that "
            "stale metrics are never mistaken for an all-clear condition. Visual design "
            "prioritizes operator clarity over decorative complexity."
        )
    )
    story.append(H("3.10.3 Operational CLI Interfaces", "SubSectionHead"))
    story.append(
        P(
            "Guardian supports --console, --simulate, and --demo flags. Demo mode runs "
            "a scripted escalation path intended to exercise CRITICAL lockdown under "
            "expected scoring. Simulate mode keeps synthetic telemetry alive for "
            "interactive UI walkthroughs. PowerShell scripts wrap setup, demo launch, "
            "service install/uninstall, and ACL hardening."
        )
    )

    story.append(H("3.11 SECURITY AND ACCESS CONTROL"))
    story.append(
        P(
            "MayaJaal’s trust model is local Windows identity rather than application "
            "usernames and passwords. SecurePipeFactory configures named-pipe ACLs for "
            "LocalSystem, Administrators, and the current user SID, with an additional "
            "connected-client authorization check. Vault passwords never persist in "
            "plaintext. Wrapping keys are derived with Argon2id; item encryption uses "
            "AES-256-GCM. Bootstrap secrets can be protected with DPAPI. "
            "harden-acls.ps1 can strip inheritance on ProgramData\\MayaJaal and apply "
            "least-privilege patterns for evidence directories."
        )
    )
    story.append(
        P(
            "Residual risks remain, as with any local user-mode agent: a privileged "
            "attacker on the same machine can interfere with services and files. "
            "MayaJaal does not claim kernel-level tamper resistance. Its security value "
            "is strongest as a transparent correlation and reversible-containment layer "
            "with auditable math and durable evidence."
        )
    )

    story.append(H("3.12 INCIDENT LIFECYCLE DESIGN"))
    story.append(
        P(
            "Incidents transition through statuses such as open, active containment, and "
            "resolved depending on response execution and operator commands. Evidence is "
            "attached at containment time so later resolution does not erase the "
            "technical trail. Rollback plans associated with SafetyGate-approved actions "
            "acknowledge that automated defense should be reversible when safe to undo. "
            "Incident numbering uses a date-based scheme for easy human reference during "
            "demos and viva examinations."
        )
    )

    story.append(H("3.13 OPERATOR EXPERIENCE DESIGN PRINCIPLES"))
    story.append(
        P(
            "Security Center design favors immediate comprehension: threat level and "
            "confidence should be visible without navigating deep menus; offline state "
            "must never look like “all clear”; event timelines should help narrate why a "
            "pattern fired. These principles mirror real SOC usability concerns even "
            "though the product scale is intentionally small. The console is therefore "
            "designed as an operator instrument rather than a marketing dashboard."
        )
    )

    story.append(H("3.14 SUMMARY OF SYSTEM DESIGN"))
    story.append(
        P(
            "The design separates collection, pure scoring, persistence and "
            "cryptography, host orchestration, and operator UI. Deception provides "
            "high-value signals; engines compose weak signals into confidence; policy "
            "and safety gate responses; vaults and evidence make containment and review "
            "concrete. Chapter 4 describes how this design is realized in code and "
            "verified through testing and demonstration."
        )
    )
    story.append(PageBreak())
    return story


def build_chapter4(api):
    P, H, bullets, numbered, make_table = (
        api["P"],
        api["H"],
        api["bullets"],
        api["numbered"],
        api["make_table"],
    )
    set_chapter = api["set_chapter"]
    set_chapter("IMPLEMENTATION")
    story = []
    story.append(P("CHAPTER 4: IMPLEMENTATION", "ChapterTitle"))

    story.append(H("4.1 TECHNOLOGY STACK"))
    story.append(
        P(
            "MayaJaal is implemented as a multi-project .NET 8 solution using C# 12. "
            "The stack was selected for Windows fidelity, strong typing, asynchronous "
            "I/O, and mature libraries for cryptography, persistence, and desktop UI. "
            "Figure 4.1 conceptually groups technologies by role."
        )
    )
    story.append(P("Figure 4.1: Technology Stack Overview", "Caption"))

    story.append(H("4.1.1 Language and Runtime", "SubSectionHead"))
    story.append(
        P(
            "C# 12 on .NET 8 provides modern language features, nullable reference "
            "types, and async/await for collectors and IPC. Library projects target "
            "net8.0 where possible; Guardian and Security Center target net8.0-windows "
            "to access Windows-specific hosting and WPF APIs. Directory.Build.props "
            "centralizes product metadata such as company name, product name, and "
            "version 1.0.0."
        )
    )
    story.append(H("4.1.2 Guardian Host and Backend Services", "SubSectionHead"))
    story.append(
        P(
            "Microsoft.Extensions.Hosting and Windows Services packages host Guardian. "
            "Dependency injection registers Domain engines and Infrastructure services "
            "through composition helpers, while Guardian registers ThreatEngine, "
            "collectors, IpcServer, and bootstrap components as hosted services. This "
            "approach standardizes lifetime management and graceful shutdown."
        )
    )
    story.append(H("4.1.3 Security Center and UI Toolkit", "SubSectionHead"))
    story.append(
        P(
            "WPF with CommunityToolkit.Mvvm implements the operator console using MVVM. "
            "Observable properties and commands keep view code thin. "
            "GuardianStatusService coordinates polling and updates viewmodels bound to "
            "XAML views, including dashboard, incidents, vaults, and policies."
        )
    )
    story.append(H("4.1.4 Persistence, Logging, and Reporting", "SubSectionHead"))
    story.append(
        P(
            "Microsoft.Data.Sqlite stores events locally without a separate database "
            "server. Serilog writes console and rolling file logs under ProgramData for "
            "diagnostics. QuestPDF generates incident PDF reports for evidence and "
            "archival workflows associated with containment."
        )
    )
    story.append(H("4.1.5 Cryptography", "SubSectionHead"))
    story.append(
        P(
            "AES-256-GCM protects vault items and wrapped keys. Argon2id through "
            "Konscious.Security.Cryptography.Argon2 derives wrapping keys from "
            "passwords. SHA-256 supports hash chaining. DPAPI protects bootstrap "
            "secrets on the local machine. These choices reflect current practical "
            "guidance for authenticated encryption and password-based key derivation."
        )
    )
    story.append(H("4.1.6 Testing, CI, and Deployment Tools", "SubSectionHead"))
    story.append(
        P(
            "xUnit validates Domain engines. GitHub Actions on windows-latest restores, "
            "builds, and tests the solution. PowerShell scripts publish binaries, create "
            "the MayaJaal Guardian service via sc.exe, manage Start Menu shortcuts, "
            "uninstall cleanly, and harden directory ACLs. An installer folder is "
            "reserved for future MSI packaging."
        )
    )
    story.append(P("Table 4.1: Technology Stack and Tools", "Caption"))
    story.append(
        make_table(
            ["Area", "Technologies"],
            [
                ["Language/Runtime", ".NET 8, C# 12"],
                ["UI", "WPF, CommunityToolkit.Mvvm"],
                ["Host", "Microsoft.Extensions.Hosting, Windows Services"],
                ["IPC", "Named pipes, length-prefixed JSON"],
                ["Database", "SQLite via Microsoft.Data.Sqlite"],
                ["Crypto", "AES-GCM, Argon2id, SHA-256, DPAPI"],
                ["Logging / PDF", "Serilog, QuestPDF"],
                ["Tests / CI", "xUnit, GitHub Actions (windows-latest)"],
                ["Ops Scripts", "PowerShell, sc.exe, dotnet publish"],
            ],
            col_widths=[2.0 * inch, 4.5 * inch],
        )
    )
    story.append(PageBreak())

    story.append(H("4.2 MODULE DEVELOPMENT"))
    story.append(H("4.2.1 Shared Models and IPC Client", "SubSectionHead"))
    story.append(
        P(
            "MayaJaal.Shared defines SecurityEvent and related enums including "
            "EventType, EventSource, EventSeverity, ThreatLevel, IncidentStatus, and "
            "ResponseAction. Nested contexts capture user, process, file, and device "
            "details. GuardianClient implements the client side of the pipe protocol "
            "used by Security Center. Keeping these types shared prevents UI/host "
            "contract drift and documents the language spoken across process boundaries."
        )
    )
    story.append(H("4.2.2 Domain Engines Module", "SubSectionHead"))
    story.append(
        P(
            "RiskEngine, ConfidenceEngine, CorrelationEngine, PolicyEngine, and "
            "SafetyGate were implemented as deterministic components with no file or "
            "network I/O. This enabled focused unit tests for empty windows, honey "
            "elevation, confidence gating, decay behavior, combine-risk behavior, and "
            "named pattern detection. Normative constants—weights, multipliers, and "
            "thresholds—live beside the algorithms they govern."
        )
    )
    story.append(H("4.2.3 Infrastructure Module", "SubSectionHead"))
    story.append(
        P(
            "EventStore creates or opens events.db and appends chained events. "
            "VaultService and KeyManager implement create, unlock, lock, and encrypt "
            "flows. HoneyFileService writes decoy templates and maintains catalog "
            "metadata. IncidentService coordinates incident lifecycle and evidence. "
            "ReportGenerator produces PDFs. Dependency injection centralizes service "
            "registration for Guardian consumption."
        )
    )
    story.append(H("4.2.4 Guardian Collectors and ThreatEngine", "SubSectionHead"))
    story.append(
        P(
            "EventCollector, UsbCollector, and ProcessCollector run as hosted "
            "background work. ProcessAllowlist reduces noise. ThreatEngine integrates "
            "Domain and Infrastructure collaborators into the pipeline described in "
            "Chapter 3. DemoScenarioRunner provides a deterministic escalation path; "
            "simulate mode injects ongoing demo telemetry while keeping IPC available "
            "for the console. Vault bootstrap ensures containment capacity exists "
            "before incidents occur."
        )
    )
    story.append(H("4.2.5 IPC Server and Pipe Security", "SubSectionHead"))
    story.append(
        P(
            "IpcServer accepts connections on MayaJaal.Guardian and dispatches commands "
            "to Guardian services. SecurePipeFactory applies ACLs and performs "
            "connected-client authorization checks. Expanded commands cover vaults, "
            "incidents, policies, resolve, and rollback in addition to status and "
            "events, enabling a richer Security Center without widening the UI’s "
            "dependency surface beyond Shared contracts."
        )
    )
    story.append(H("4.2.6 Security Center Module", "SubSectionHead"))
    story.append(
        P(
            "MainWindow.xaml and related views present dashboard, incidents, vault, and "
            "policy pages. GuardianStatusService polls multiple GET_* commands and "
            "updates observable state. Offline detection drives a banner so operators "
            "know when metrics are stale. Figure 4.2 represents the dashboard concept "
            "emphasizing threat level, risk, confidence, active pattern, recent events, "
            "and incident summary."
        )
    )
    story.append(P("Figure 4.2: Security Center Dashboard Concept", "Caption"))
    story.append(H("4.2.7 Operations and Demo Scripts", "SubSectionHead"))
    story.append(
        P(
            "scripts\\setup.ps1 restores, builds, and tests. scripts\\run-demo.ps1 "
            "launches Guardian with --console --simulate and then Security Center. "
            "install-service.ps1 and uninstall-service.ps1 manage Windows Service "
            "installation under Program Files. harden-acls.ps1 tightens ProgramData "
            "permissions after deployment. These scripts make the system operable by "
            "evaluators who are not familiar with every project file."
        )
    )
    story.append(P("Table 4.2: Module Description", "Caption"))
    story.append(
        make_table(
            ["Module", "Description"],
            [
                ["Shared IPC/Models", "Cross-process contracts and security domain types"],
                ["Domain Engines", "Risk, confidence, correlation, policy, safety"],
                ["EventStore", "SQLite persistence with hash chain"],
                ["Vault/Crypto", "Argon2id and AES-GCM containment"],
                ["Honey/Collectors", "Decoys and file/USB/process telemetry"],
                ["ThreatEngine", "End-to-end scoring and response orchestration"],
                ["IpcServer", "Named-pipe command surface"],
                ["Security Center", "Operator visualization and control"],
                ["Scripts/CI", "Setup, demo, service install, automated build/test"],
            ],
            col_widths=[2.0 * inch, 4.5 * inch],
        )
    )
    story.append(PageBreak())

    story.append(H("4.3 TESTING"))
    story.append(
        P(
            "Testing focused on correctness of scoring logic, reliability of build and "
            "demo paths, and practical verification of escalation behavior. Because "
            "Domain engines are pure, they received automated unit tests. Host, IPC, "
            "and UI paths were verified through controlled manual and demo scenarios, "
            "while CI ensured Release builds and tests remain green."
        )
    )
    story.append(H("4.3.1 Unit Testing", "SubSectionHead"))
    story.append(
        P(
            "xUnit tests cover RiskEngine (empty risk, honey elevation, decay, combine), "
            "ConfidenceEngine (gates and theory cases), and CorrelationEngine (pattern "
            "detection and relatedness). These cases form the regression net for scoring "
            "math and are executed locally through setup scripts and in CI."
        )
    )
    story.append(H("4.3.2 Integration and Host Testing", "SubSectionHead"))
    story.append(
        P(
            "Integration-style verification uses Guardian --demo and --console "
            "--simulate together with Security Center. These runs validate collector "
            "emission or injection, IPC availability, UI refresh, and incident/lockdown "
            "side effects on a developer workstation."
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
    story.append(H("4.3.5 Performance Considerations", "SubSectionHead"))
    story.append(
        P(
            "Performance measures include bounded threat-window size, approximately "
            "two-second UI polling, collector poll intervals of a few seconds, and "
            "avoidance of unbounded UI collections. On typical student hardware, demo "
            "escalation remains interactive without noticeable UI freezing."
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
            "Several challenges arose during implementation. Coordinating asynchronous "
            "collectors with a consistent event model required careful normalization of "
            "timestamps, paths, and severity. Keeping Security Center responsive "
            "required clear separation between IPC awaits and observable property "
            "updates. Designing confidence thresholds that both demo reliably and avoid "
            "trivial false lockdowns required iterative tuning against simulate and demo "
            "scenarios."
        )
    )
    story.append(
        P(
            "Pipe security and Windows Service permissions also demanded attention. A "
            "pipe that is too open weakens local trust assumptions; a pipe that is too "
            "closed breaks student demos under common login contexts. The chosen ACL "
            "pattern balances LocalSystem, Administrators, and current-user access. "
            "Documenting formulas in parallel with code proved essential to prevent "
            "drift between Capstone narrative and implementation constants."
        )
    )

    story.append(H("4.6 CONFIGURATION AND DIRECTORY LAYOUT"))
    story.append(
        P(
            "On a running system, MayaJaal uses %ProgramData%\\MayaJaal for logs, data, "
            "vaults, incidents, evidence, and configuration, while decoys live under the "
            "user Documents tree to remain attractive and realistic. Service installs "
            "publish binaries under Program Files\\MayaJaal. This split between mutable "
            "state and program binaries follows common Windows application practice and "
            "simplifies both ACL hardening and uninstallation."
        )
    )
    story.append(
        P(
            "Configuration is intentionally minimal for Capstone scope: hosting flags "
            "control console, simulate, and demo behavior; engine constants live in "
            "Domain code; UI polling intervals live in Security Center services. Future "
            "versions may externalize policy packs as JSON for operator tuning without "
            "recompilation."
        )
    )

    story.append(H("4.7 CONTINUOUS INTEGRATION WORKFLOW"))
    story.append(
        P(
            "The GitHub Actions workflow runs on windows-latest with the .NET 8 SDK. It "
            "restores the solution, builds Release, and runs tests. This provides a "
            "baseline guarantee that pull requests do not silently break engine math or "
            "project compilation. Although UI automation is not yet in CI, the workflow "
            "still catches a large class of regressions early and documents an "
            "expectable verification path for evaluators."
        )
    )
    story.append(PageBreak())
    return story


def build_chapter5(api):
    P, H, bullets, numbered, make_table = (
        api["P"],
        api["H"],
        api["bullets"],
        api["numbered"],
        api["make_table"],
    )
    set_chapter = api["set_chapter"]
    set_chapter("EVALUATION AND FUTURE WORK")
    story = []
    story.append(P("CHAPTER 5: EVALUATION AND FUTURE WORK", "ChapterTitle"))

    story.append(H("5.1 EVALUATION OF RESULTS"))
    story.append(
        P(
            "MayaJaal was evaluated against the requirements defined during system "
            "analysis and the design commitments documented in Chapter 3. Evaluation "
            "considered functional completeness, scoring transparency, operator "
            "usability, local security controls, and verification evidence obtained from "
            "unit tests, Release builds, and demo runs. The objective was not to claim "
            "commercial EDR parity, but to determine whether the Capstone prototype "
            "honestly delivers a complete local detect–decide–contain narrative."
        )
    )

    story.append(H("5.1.1 Functional Evaluation", "SubSectionHead"))
    story.append(
        P(
            "Deception and collectors successfully produce typed events from honey "
            "access, file activity, USB changes, and suspicious processes. ThreatEngine "
            "orchestrates persistence, scoring, correlation, policy, safety, and "
            "incident response as an end-to-end pipeline. Security Center displays live "
            "state and supports key operator workflows, including offline indication when "
            "Guardian is unavailable. Demo mode provides a repeatable CRITICAL "
            "escalation narrative that exercises nearly every major module within a "
            "classroom-friendly time budget."
        )
    )
    story.append(
        P(
            "Vault lockdown and evidence packaging were verified as concrete side "
            "effects of approved high-confidence responses. Incidents receive durable "
            "identifiers and artifact directories under ProgramData, enabling later review "
            "during viva examination or after-action discussion. These outcomes confirm "
            "that MayaJaal is not limited to alert text; it produces operator-visible "
            "state changes and reviewable evidence."
        )
    )

    story.append(H("5.1.2 Scoring and Test Evaluation", "SubSectionHead"))
    story.append(
        P(
            "Domain unit tests covering RiskEngine, ConfidenceEngine, and "
            "CorrelationEngine passed in Release configuration. This supports the claim "
            "that normative formulas are executable and regression-protected rather than "
            "merely descriptive. Tests exercise empty windows, honey elevation, decay, "
            "combine-risk behavior, confidence gates, and named pattern detection. The "
            "educational value of explicit math—inspectable in source and validated by "
            "tests—is one of the project’s strongest outcomes."
        )
    )

    story.append(H("5.1.3 Security and Operations Evaluation", "SubSectionHead"))
    story.append(
        P(
            "AES-GCM vault lockdown, Argon2id wrapping, hash-chained SQLite events, "
            "pipe ACLs, and ACL hardening scripts collectively demonstrate a thoughtful "
            "local trust model. Service install scripts show a path from classroom demo "
            "to workstation service deployment, even though full MSI packaging remains "
            "future work. Operationally, console, simulate, demo, and service modes "
            "provide flexibility for development, presentation, and more persistent local "
            "use."
        )
    )

    story.append(P("Table 5.1: Evaluation of Major Modules", "Caption"))
    story.append(
        make_table(
            ["Module", "Evaluation Outcome"],
            [
                ["Honey and Collectors", "Meets scope; emits actionable local telemetry"],
                ["Risk / Confidence / Correlation", "Meets scope; unit-tested formulas"],
                ["Policy and SafetyGate", "Meets scope; confidence-gated containment"],
                ["Vault and Evidence", "Meets scope; reversible lockdown and artifacts"],
                ["Security Center", "Meets scope; live IPC visualization"],
                ["Service / Scripts / CI", "Partially mature; MSI packaging still future work"],
            ],
            col_widths=[2.4 * inch, 4.1 * inch],
        )
    )

    story.append(H("5.2 FUTURE ENHANCEMENTS"))
    story.append(
        P(
            "Future work should extend MayaJaal without abandoning its local-first and "
            "explainable-security principles. The following enhancements are prioritized "
            "by expected defensive value and engineering readiness."
        )
    )
    story.append(H("5.2.1 Deeper Telemetry Sensors", "SubSectionHead"))
    story.append(
        P(
            "Integration of richer ETW providers, process ancestry, and optional kernel "
            "minifilter callbacks would reduce reliance on polling and improve visibility "
            "into stealthier activity. Such sensors must be introduced carefully because "
            "they increase installation complexity and privilege requirements."
        )
    )
    story.append(H("5.2.2 Continuous Integrity Verification", "SubSectionHead"))
    story.append(
        P(
            "A background hash-chain verifier job can periodically validate EventStore "
            "integrity and alert on breaks, strengthening forensic assurance beyond "
            "append-time hashing alone."
        )
    )
    story.append(H("5.2.3 Expanded Automated Testing", "SubSectionHead"))
    story.append(
        P(
            "Integration tests for IPC framing, vault round-trips, collector adapters, "
            "and UI viewmodels would increase release confidence beyond Domain unit "
            "tests and reduce dependence on manual demo verification."
        )
    )
    story.append(H("5.2.4 Packaging and Enterprise Operations", "SubSectionHead"))
    story.append(
        P(
            "MSI or MSIX installers, signed binaries, central policy packs, and optional "
            "SIEM export connectors would help transition from prototype to managed "
            "fleet use while preserving the local-first core."
        )
    )
    story.append(H("5.2.5 Explainable Analytics Complements", "SubSectionHead"))
    story.append(
        P(
            "Optional anomaly models could complement correlation patterns if features "
            "and false-positive behavior remain explainable to operators. Opaque scores "
            "without narrative value would conflict with the Capstone’s transparency "
            "goal and should be avoided."
        )
    )
    story.append(H("5.2.6 Cross-Host Deception Fabric", "SubSectionHead"))
    story.append(
        P(
            "Coordinated decoys across multiple laboratory machines could support "
            "blue-team exercises, provided privacy, consent, and administrative "
            "boundaries remain explicit."
        )
    )
    story.append(H("5.2.7 Usability and Accessibility Polish", "SubSectionHead"))
    story.append(
        P(
            "Guided onboarding, clearer incident narratives, and accessibility "
            "improvements in Security Center would broaden operator audiences in "
            "classrooms and SOC-training environments."
        )
    )

    story.append(H("5.3 STRENGTHS AND LIMITATIONS"))
    story.append(P("Principal strengths observed during evaluation include:"))
    story.extend(
        bullets(
            [
                "Explainable scoring suitable for academic assessment and unit testing",
                "End-to-end local demonstration without cloud services",
                "Reversible cryptographic containment rather than destructive response",
                "Clean Architecture separating Domain math from I/O and UI",
                "Operator experience that surfaces offline and critical states clearly",
            ]
        )
    )
    story.append(P("Principal limitations acknowledged for Capstone scope include:"))
    story.extend(
        bullets(
            [
                "User-mode sensors can miss kernel-level tampering",
                "Heuristic correlation may false-positive on unusual legitimate bulk work",
                "Automated UI and IPC tests are not yet comprehensive",
                "Fleet management and centralized policy distribution are out of scope",
                "Packaging is script-based rather than fully MSI-polished",
            ]
        )
    )

    story.append(H("5.4 REQUIREMENTS TRACEABILITY"))
    story.append(
        P(
            "Table 5.2 summarizes how major functional requirements map to implemented "
            "capabilities and evaluation evidence. This traceability supports Capstone "
            "assessment by connecting Chapter 2 requirements to concrete outcomes."
        )
    )
    story.append(P("Table 5.2: Requirements Traceability (Selected)", "Caption"))
    story.append(
        make_table(
            ["Requirement Theme", "Implementation Evidence", "Evaluation Method"],
            [
                ["Honey deception", "HoneyFileService + HONEY_* events", "Demo / functional check"],
                ["Multi-signal collection", "File/USB/process collectors", "Demo / simulate runs"],
                ["Transparent scoring", "Domain engines + unit tests", "xUnit + code review"],
                ["Safety-gated response", "PolicyEngine + SafetyGate", "Demo lockdown path"],
                ["Vault containment", "VaultService LockAll", "Incident/evidence artifacts"],
                ["Operator console", "Security Center IPC polling", "UI offline/live checks"],
                ["Local operations", "Scripts + service install", "Setup/runbook exercise"],
            ],
            col_widths=[1.9 * inch, 2.4 * inch, 2.2 * inch],
        )
    )

    story.append(H("5.5 ADOPTION RISKS AND MITIGATIONS"))
    story.append(
        P(
            "Adoption risk in real organizations includes alert fatigue and fear of "
            "accidental lockdown. Mitigations already present are confidence thresholds, "
            "SafetyGate vetoes, and reversible vault locks. Classroom adoption risk "
            "includes environment drift across student machines; mitigations include "
            "setup scripts, demo mode, and documented ProgramData layouts. These "
            "mitigations do not eliminate risk, but they make residual risk explicit and "
            "manageable within Capstone scope."
        )
    )

    story.append(H("5.6 SUMMARY"))
    story.append(
        P(
            "Evaluation indicates that MayaJaal meets its Capstone objectives as a "
            "local-first deception-and-response prototype with transparent scoring, "
            "practical containment, and an operator console. Future enhancements are "
            "natural extensions of a working core rather than repairs of a broken "
            "foundation. Chapter 6 consolidates conclusions and discusses the broader "
            "significance of the design choices."
        )
    )
    story.append(PageBreak())
    return story


def build_chapter6(api):
    P, H, bullets = api["P"], api["H"], api["bullets"]
    set_chapter = api["set_chapter"]
    set_chapter("CONCLUSION AND DISCUSSION")
    story = []
    story.append(P("CHAPTER 6: CONCLUSION AND DISCUSSION", "ChapterTitle"))

    story.append(H("6.1 CONCLUSION"))
    story.append(
        P(
            "MayaJaal – Local-First Cyber Deception and Endpoint Defense Platform was "
            "developed to demonstrate how deception artifacts, behavioral scoring, "
            "policy and safety controls, cryptography, and operator visualization can be "
            "combined into a coherent Windows security prototype. The work integrates a "
            "Guardian host and a Security Center console into one explainable defensive "
            "narrative suitable for Capstone evaluation."
        )
    )
    story.append(
        P(
            "Guardian collects file, USB, and process signals; scores a sliding threat "
            "window using Risk, Confidence, and Correlation engines; selects responses "
            "through PolicyEngine; gates them with SafetyGate; and persists events and "
            "incidents with hash-chained SQLite storage and vault-backed containment. "
            "Security Center provides live situational awareness over named-pipe IPC and "
            "makes offline or critical states unmistakable to an operator."
        )
    )
    story.append(
        P(
            "From a software-engineering perspective, the project applies Clean "
            "Architecture principles, dependency injection, MVVM, automated Domain "
            "testing, and scripted operations. From a security-engineering perspective, "
            "it emphasizes explainable math, reversible containment, and explicit trust "
            "boundaries rather than opaque cloud scoring alone. The Capstone "
            "implementation does not claim to replace commercial antivirus or EDR "
            "products. Instead, it shows that a student team can design and build a "
            "meaningful local defense story: weak signals become strong when correlated "
            "with honey access; high-impact actions require confidence; and evidence "
            "should remain durable and reviewable."
        )
    )
    story.append(
        P(
            "The modular architecture provides a foundation for future enhancements such "
            "as deeper telemetry, continuous integrity verification, broader automated "
            "tests, professional packaging, and optional explainable analytics. Overall, "
            "MayaJaal successfully fulfills the objectives of Capstone Project – I by "
            "delivering a working, documented, and demonstrable cyber-defense platform."
        )
    )

    story.append(H("6.2 DISCUSSION OF DESIGN TRADE-OFFS"))
    story.append(
        P(
            "Several deliberate trade-offs shaped the final system. Transparency was "
            "preferred over opaque detection power so formulas remain teachable and "
            "testable. Local-first operation was preferred over cloud analytics to "
            "support offline laboratories and clear trust boundaries. Safety-gated, "
            "reversible containment was preferred over immediate destructive response to "
            "reduce harm from false positives. User-mode collectors were preferred over "
            "kernel drivers to keep installation and demonstration practical within "
            "Capstone constraints."
        )
    )
    story.append(
        P(
            "These trade-offs are not free. Transparency can miss stealthy techniques "
            "that proprietary models might catch. Local-first design forgoes fleet-wide "
            "learning. Reversible lockdown is less absolute than destructive wipe. "
            "User-mode sensors can be evaded by privileged malware. The Capstone accepts "
            "these costs in exchange for an honest, inspectable, and demonstrable system."
        )
    )

    story.append(H("6.3 COMPARISON WITH RELATED APPROACHES"))
    story.append(
        P(
            "Relative to antivirus, MayaJaal is complementary: it correlates behavior "
            "and deception rather than primarily classifying binaries. Relative to "
            "classic EDR, it is narrower and local, but more transparent for teaching. "
            "Relative to network honeypots, it focuses on endpoint document pathways. "
            "Relative to DLP, it uses decoy touch and mass-activity correlation rather "
            "than deep content classification. These comparisons clarify that MayaJaal "
            "occupies a deception-assisted local correlation and containment niche."
        )
    )

    story.append(H("6.4 ETHICAL AND OPERATIONAL CONSIDERATIONS"))
    story.append(
        P(
            "Deception files must remain synthetic and must not trick legitimate "
            "collaborators in harmful ways. In shared laboratory machines, operators "
            "should disclose monitoring or deception software when required by policy. "
            "Automated lockdown should be demonstrated carefully so coursework or "
            "personal files are not unexpectedly made inaccessible. Evidence packs may "
            "contain paths and event metadata and should therefore be ACL-hardened; "
            "logs must never include vault passwords or raw master keys."
        )
    )

    story.append(H("6.5 MAPPING TO LEARNING OUTCOMES"))
    story.append(
        P(
            "The project maps to several Bachelor of Technology learning outcomes. "
            "Requirements analysis appears in Chapter 2. Architectural design and "
            "modular decomposition appear in Chapter 3. Implementation with modern "
            "tooling appears in Chapter 4. Verification through tests and demos "
            "supports quality-assurance outcomes. Documentation discipline—both this "
            "Capstone report and the internal engineering specification—supports "
            "professional communication outcomes."
        )
    )
    story.append(
        P(
            "Cross-cutting concepts practiced include concurrency and hosting, "
            "inter-process communication, cryptography, data persistence, UI state "
            "management, scripting and DevOps basics, and threat-informed design. "
            "Taken together, these provide a stronger integrative experience than "
            "implementing any single API sample in isolation."
        )
    )

    story.append(H("6.6 LESSONS LEARNED"))
    story.extend(
        bullets(
            [
                "Pure Domain engines dramatically improve testability and documentation quality.",
                "Deterministic demo and simulate modes are essential for grading and presentation reliability.",
                "IPC contracts should evolve carefully as UI features expand.",
                "Safety gates are as important as detection logic when actions affect user data.",
                "Clear ProgramData layouts simplify both operations and report writing.",
                "Writing normative formulas early prevents magic-number drift across modules.",
            ]
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
    story.append(PageBreak())
    return story


def build_appendix(api):
    P, H, bullets, numbered, make_table = (
        api["P"],
        api["H"],
        api["bullets"],
        api["numbered"],
        api["make_table"],
    )
    set_chapter = api["set_chapter"]
    set_chapter("Appendix")
    story = []
    story.append(P("Appendix", "ChapterTitle"))

    story.append(H("A. SOURCE CODE REPOSITORY"))
    story.append(
        P(
            "The MayaJaal source code is organized as a .NET solution with projects "
            "under src\\ and tests\\, documentation under docs\\, and operational "
            "scripts under scripts\\. Principal paths include MayaJaal.sln, "
            "Directory.Build.props, src\\MayaJaal.Guardian, "
            "src\\MayaJaal.SecurityCenter, src\\MayaJaal.Domain\\Engines, "
            "src\\MayaJaal.Infrastructure, tests\\MayaJaal.Tests, "
            "docs\\MAYAJAAL-COMPREHENSIVE-REPORT.md, docs\\architecture.md, and "
            ".github\\workflows\\build.yml."
        )
    )

    story.append(H("B. DEMO AND IPC VERIFICATION"))
    story.append(
        P(
            "Verification during development included Guardian demo and simulate runs "
            "together with Security Center IPC polling. Command classes exercised "
            "included status, events, incidents, assets, vaults, policies, lock, "
            "resolve, rollback, and ping. Evaluators may insert screenshots of Security "
            "Center and console output in hardcopy submissions as needed."
        )
    )

    story.append(H("C. SAMPLE STORAGE STRUCTURE"))
    story.append(
        make_table(
            ["Location / Entity", "Purpose"],
            [
                ["%ProgramData%\\MayaJaal\\Data\\events.db", "Hash-chained security events"],
                ["%ProgramData%\\MayaJaal\\Vaults", "Encrypted vault containers"],
                ["%ProgramData%\\MayaJaal\\Incidents", "Incident records"],
                ["%ProgramData%\\MayaJaal\\Evidence", "Evidence packs and reports"],
                ["%ProgramData%\\MayaJaal\\Logs", "Serilog rolling logs"],
                ["%ProgramData%\\MayaJaal\\Config", "Honey catalog, assets, bootstrap secrets"],
                ["%USERPROFILE%\\Documents\\MayaJaal-Decoys", "Honey decoy files"],
            ],
            col_widths=[3.3 * inch, 3.2 * inch],
        )
    )

    story.append(H("D. BUILD AND RUN COMMANDS"))
    story.extend(
        bullets(
            [
                "Setup: .\\scripts\\setup.ps1",
                "Guardian console: dotnet run --project src\\MayaJaal.Guardian -- --console",
                "Simulate: add --simulate",
                "Demo: add --demo",
                "Security Center: dotnet run --project src\\MayaJaal.SecurityCenter",
                "Live demo wrapper: .\\scripts\\run-demo.ps1",
                "Service install (administrator): .\\scripts\\install-service.ps1",
            ]
        )
    )
    story.append(PageBreak())
    return story


def build_technical_annex(api):
    P, H, bullets, numbered, make_table = (
        api["P"],
        api["H"],
        api["bullets"],
        api["numbered"],
        api["make_table"],
    )
    set_chapter = api["set_chapter"]
    set_chapter("Appendix")
    story = []

    story.append(H("E. THREAT SCENARIOS"))
    story.append(
        P(
            "Six scenarios guided requirements and acceptance planning. Each row "
            "captures a distinct detection story rather than restating a single USB "
            "narrative."
        )
    )
    story.append(
        make_table(
            ["ID", "Narrative", "Key Signals", "Intended Response"],
            [
                ["A", "USB exfil with decoy probe", "USB, process, honey, mass", "CONTAIN / LOCKDOWN"],
                ["B", "Insider harvest without USB", "Honey, copy, mass", "ALERT / CONTAIN"],
                ["C", "Ransomware-like churn", "Mass + ransomware heuristic", "LOCKDOWN if high conf."],
                ["D", "Accidental bulk delete", "Deletes/mass, no honey", "Usually confidence-gated"],
                ["E", "Operator drill", "Demo/simulate inject", "Deterministic walkthrough"],
                ["F", "Vault containment path", "Approved response", "LockAll + evidence"],
            ],
            col_widths=[0.6 * inch, 2.0 * inch, 2.0 * inch, 1.9 * inch],
        )
    )
    story.append(PageBreak())

    story.append(H("F. RISK SIGNAL WEIGHT TABLE"))
    story.append(
        P(
            "Table F.1 lists minimum/maximum base contribution ranges and decay "
            "constants (λ per minute) used by RiskEngine for major EventType values."
        )
    )
    story.append(P("Table F.1: Signal Weights (Min, Max, λ)", "Caption"))
    story.append(
        make_table(
            ["EventType", "Min", "Max", "λ / min"],
            [
                ["FILE_ACCESS", "1", "5", "0.05"],
                ["FILE_MODIFY", "5", "15", "0.04"],
                ["FILE_COPY / FILE_MOVE", "10", "25", "0.03"],
                ["FILE_DELETE", "15", "30", "0.02"],
                ["MASS_FILE_ACTIVITY", "30", "50", "0.01"],
                ["PROCESS_START", "5", "20", "0.04"],
                ["USB_INSERT", "5", "15", "0.03"],
                ["USB_FILE_ACCESS", "15", "35", "0.02"],
                ["HONEY_ACCESS", "50", "80", "0.005"],
                ["HONEY_MODIFY", "60", "90", "0.005"],
                ["RANSOMWARE_BEHAVIOR", "80", "100", "0.001"],
            ],
            col_widths=[2.4 * inch, 1.2 * inch, 1.2 * inch, 1.7 * inch],
        )
    )
    story.append(
        P(
            "Context multipliers further scale contributions: honey access/modify ×1.5, "
            "suspicious process ×1.4, USB device ×1.2, protected asset ×1.15. Honey "
            "events also enforce a base-weight floor of 55. Final risk is "
            "round(total) soft-capped at 200."
        )
    )
    story.append(PageBreak())

    story.append(H("G. WORKED RISK EXAMPLE"))
    story.append(
        P(
            "Consider a single HONEY_ACCESS event with RiskContribution=70, "
            "IsHoney=true, SensitivityLevel=9, Severity=CRITICAL, and Δt≈0."
        )
    )
    story.extend(
        bullets(
            [
                "baseWeight = max(70, 55) = 70",
                "C (confidence factor for honey) ≈ 0.9",
                "A (sensitivity scale) = 9/5 = 1.8",
                "X (honey type multiplier) = 1.5",
                "decay term ≈ e^0 = 1.0",
                "contribution ≈ 70 × 0.9 × 1.8 × 1.5 × 1.0 = 170.1 → Value ≈ 170",
            ]
        )
    )
    story.append(
        P(
            "If overall confidence is high, DetermineThreatLevel maps this into "
            "CRITICAL. If confidence is very low (&lt;0.35) while raw risk is high, "
            "banding caps effective risk near 50 so uncertain stories do not "
            "immediately look CRITICAL. Unit tests cover both branches."
        )
    )

    story.append(H("H. CONFIDENCE MIX AND ACTION THRESHOLDS"))
    story.append(
        P(
            "ConfidenceEngine computes Clamp(0.4·SignalStrength + 0.3·CorrelationQuality "
            "+ 0.2·ContextConsistency + 0.1·HistoricalAccuracy). CorrelationQuality "
            "climbs as USB, honey, mass, and suspicious process evidence co-occur. "
            "ContextConsistency is higher when events fall inside a short time span."
        )
    )
    story.append(P("Table H.1: Confidence Thresholds by ResponseAction", "Caption"))
    story.append(
        make_table(
            ["ResponseAction", "Confidence Threshold"],
            [
                ["NONE / MONITOR", "0.0"],
                ["ALERT / NOTIFY_USER", "0.5"],
                ["LOCK_VAULT / RESTRICT_ASSETS / PRESERVE_EVIDENCE", "0.7"],
                ["CONTAIN", "0.8"],
                ["EMERGENCY_LOCKDOWN", "0.9"],
            ],
            col_widths=[4.0 * inch, 2.5 * inch],
        )
    )
    story.append(
        P(
            "Note for examiners: SafetyGate and PolicyEngine use related but not "
            "identical numeric gates. Auditors should treat Domain source files as the "
            "authoritative reference during code review."
        )
    )
    story.append(PageBreak())

    story.append(H("I. CORRELATION PATTERN CATALOG"))
    story.append(P("Table I.1: Named Set-Membership Patterns", "Caption"))
    story.append(
        make_table(
            ["Pattern", "Required Types", "TypicalRisk"],
            [
                ["USB_Honey", "USB_INSERT, HONEY_ACCESS", "70"],
                ["Honey_MassCopy", "HONEY_ACCESS, MASS_FILE_ACTIVITY", "100"],
                ["USB_Process_MassCopy", "USB_INSERT, PROCESS_START, MASS_FILE_ACTIVITY", "130"],
                ["Insider_Credential_Exfil", "HONEY_ACCESS, FILE_COPY, MASS_FILE_ACTIVITY", "150"],
                ["Ransomware_Like", "MASS_FILE_ACTIVITY, RANSOMWARE_BEHAVIOR", "160"],
            ],
            col_widths=[2.2 * inch, 3.3 * inch, 1.0 * inch],
        )
    )
    story.append(
        P(
            "If an event is marked IsHoney but typed as an ordinary FILE_* operation, "
            "CorrelationEngine normalizes it to HONEY_ACCESS for pattern matching so "
            "decoy touches are not missed due to collector typing differences."
        )
    )

    story.append(H("J. VAULT CRYPTO PARAMETERS"))
    story.append(
        make_table(
            ["Parameter", "Value / Notes"],
            [
                ["Item encryption", "AES-256-GCM"],
                ["IV size", "12 bytes"],
                ["KDF", "Argon2id"],
                ["KDF memory / iterations / parallelism", "65536 / 3 / 1 (design defaults)"],
                ["Master key", "Random 32-byte key, wrapped by KDF key"],
                ["Bootstrap secret storage", "DPAPI LocalMachine file under Config"],
                ["Vault location", "%ProgramData%\\MayaJaal\\Vaults\\{id}"],
            ],
            col_widths=[2.8 * inch, 3.7 * inch],
        )
    )
    story.append(PageBreak())

    story.append(H("K. SQLITE EVENTS TABLE AND ARTIFACT MAP"))
    story.append(
        make_table(
            ["Column / Artifact", "Role / Path"],
            [
                ["id / sequence", "Identity and append order"],
                ["timestamp / type / source / severity", "Classification fields"],
                ["is_honey / is_protected", "Deception and protection flags"],
                ["hash_chain_* / integrity_hash", "SHA-256 chain fields"],
                ["events.db", "%ProgramData%\\MayaJaal\\Data\\events.db"],
                ["Evidence", "%ProgramData%\\MayaJaal\\Evidence\\{id}\\..."],
                ["Decoys", "%USERPROFILE%\\Documents\\MayaJaal-Decoys\\"],
            ],
            col_widths=[2.8 * inch, 3.7 * inch],
        )
    )

    story.append(H("L. STRIDE REVIEW SUMMARY"))
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

    story.append(H("M. UNIT TEST MATRIX AND SOURCE CATALOG"))
    story.append(
        make_table(
            ["Suite / Area", "Coverage / Key Paths"],
            [
                ["RiskEngine", "Empty window; honey elevation; decay; combine risks"],
                ["ConfidenceEngine", "Low-signal gate; USB+honey+mass high confidence"],
                ["CorrelationEngine", "Pattern hit/miss; relatedness checks"],
                ["Domain paths", "src\\MayaJaal.Domain\\Engines\\*.cs"],
                ["Guardian paths", "ThreatEngine, Collectors, IpcServer, DemoScenarioRunner"],
                ["UI paths", "SecurityCenter ViewModels + MainWindow.xaml"],
                ["Tests", "tests\\MayaJaal.Tests\\UnitTests\\*.cs"],
            ],
            col_widths=[2.2 * inch, 4.3 * inch],
        )
    )

    story.append(H("N. HONEY DECOY TEMPLATES"))
    story.append(
        P(
            "HoneyFileService deploys six synthetic templates under "
            "Documents\\MayaJaal-Decoys. Templates contain no real secrets. Access and "
            "modify operations are remapped to high-sensitivity honey events so "
            "unauthorized interest becomes a strong signal."
        )
    )
    story.append(
        make_table(
            ["Template Category", "Defensive Intent"],
            [
                ["Financial", "Attract interest in payroll, invoices, or ledgers"],
                ["Credential", "Attract password-store or secret-file probing"],
                ["Strategy", "Attract interest in plans and confidential roadmaps"],
                ["HR", "Attract interest in personnel or offer-letter style files"],
                ["API", "Attract interest in keys, tokens, or integration notes"],
                ["Personal", "Attract opportunistic browsing of private documents"],
            ],
            col_widths=[2.0 * inch, 4.5 * inch],
        )
    )

    story.append(H("O. SECURITYEVENT FIELD SUMMARY"))
    story.append(
        make_table(
            ["Field Group", "Examples"],
            [
                ["Identity", "Id, Sequence, Timestamp"],
                ["Classification", "Type, Source, Severity"],
                ["Contexts", "User, Process, File, Device"],
                ["Scoring aids", "RiskContribution, IsHoney, IsProtected"],
                ["Integrity", "HashChainPrevious/Current, IntegrityHash"],
                ["Extensibility", "Metadata dictionary; JSON payload persistence"],
            ],
            col_widths=[2.0 * inch, 4.5 * inch],
        )
    )
    story.append(PageBreak())

    story.append(H("P. WORKED CONFIDENCE EXAMPLE"))
    story.append(
        P(
            "Consider a short window containing USB_INSERT, PROCESS_START, "
            "HONEY_ACCESS, and MASS_FILE_ACTIVITY. CorrelationQuality climbs the "
            "ladder to approximately 0.95 because USB, honey, mass, and process "
            "evidence co-occur. SignalStrength is high because honey and mass are "
            "high-value indicators. ContextConsistency is high if the span is under "
            "ten minutes (approximately 0.85). HistoricalAccuracy uses the bootstrap "
            "prior (approximately 0.75)."
        )
    )
    story.append(
        P(
            "A representative mix is therefore approximately "
            "0.4·(high signal) + 0.3·0.95 + 0.2·0.85 + 0.1·0.75, which typically "
            "exceeds 0.7 and can satisfy containment or lockdown gates depending on "
            "exact signal strength and policy thresholds. This example is consistent "
            "with unit-tested USB+honey+mass confidence behavior and with the demo "
            "escalation narrative."
        )
    )

    story.append(H("Q. GLOSSARY AND ACRONYMS"))
    for term, meaning in [
        ("Guardian", "Host that collects signals, scores threats, and serves IPC."),
        ("Security Center", "WPF console for live visualization and operator actions."),
        ("Honey / Decoy", "Synthetic sensitive-looking file used as a high-value sensor."),
        ("Threat Window", "Bounded recent-event buffer used for scoring (~15m / max 500)."),
        ("SafetyGate", "Final approval checkpoint before high-impact responses."),
        ("Vault", "Password-wrapped AES-GCM container used for containment."),
        ("Hash Chain", "SHA-256 linkage across persisted events for integrity."),
    ]:
        story.append(Paragraph(f"<b>{term}:</b> {meaning}", api["styles"]["BodyJust"]))
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

    story.append(H("R. REPRODUCTION CHECKLIST"))
    story.extend(
        numbered(
            [
                "Install .NET 8 SDK on Windows 10/11.",
                "Run scripts\\setup.ps1 (restore/build/test).",
                "Run Guardian: dotnet run --project src\\MayaJaal.Guardian -- --demo",
                "Or live UI: scripts\\run-demo.ps1 (simulate + Security Center).",
                "Confirm threat metrics and incident/evidence under ProgramData\\MayaJaal.",
                "Archive test output and demo observations for evaluation.",
            ]
        )
    )
    story.append(
        P(
            "This annex provides normative tables and worked examples that complement "
            "Chapters 1–6. It is intended for viva examination and code review, not as "
            "repeated chapter prose."
        )
    )
    story.append(PageBreak())
    return story


def build_references(api):
    P = api["P"]
    set_chapter = api["set_chapter"]
    set_chapter("Reference")
    story = []
    story.append(P("Reference", "ChapterTitle"))
    refs = [
        "Microsoft. .NET 8 Documentation. Microsoft Learn. Available at: https://learn.microsoft.com/dotnet/",
        "Microsoft. Windows Services with .NET. Microsoft Learn.",
        "Microsoft. Named Pipes in .NET. Microsoft Learn.",
        "Microsoft. Data Protection API (DPAPI). Microsoft Learn.",
        "SQLite Development Team. SQLite Documentation. Available at: https://www.sqlite.org/docs.html",
        "Microsoft. Microsoft.Data.Sqlite Documentation. Microsoft Learn.",
        "Serilog Contributors. Serilog Documentation. Available at: https://serilog.net/",
        "Community Toolkit. MVVM Toolkit Documentation. Microsoft Learn.",
        "QuestPDF Contributors. QuestPDF Documentation. Available at: https://www.questpdf.com/",
        "xUnit.net Contributors. xUnit.net Documentation. Available at: https://xunit.net/",
        "IETF RFC 5116. An Interface and Algorithms for Authenticated Encryption.",
        "Biryukov, A., Dinu, D., Khovratovich, D., & Josefsson, S. Argon2 Memory-Hard Function for Password Hashing and Proof-of-Work Applications. IETF RFC 9106.",
        "NIST. Guide to Malware Incident Prevention and Handling for Endpoints.",
        "NIST. Cybersecurity Framework. National Institute of Standards and Technology.",
        "OWASP. Desktop Application Security guidance and secure coding resources.",
        "MITRE. ATT&CK Matrix for Enterprise — Collection, Exfiltration, and Impact techniques.",
        "Spitzner, L. Honeypots: Tracking Hackers. Addison-Wesley.",
        "Provos, N., & Holz, T. Virtual Honeypots: From Botnet Tracking to Intrusion Detection. Addison-Wesley.",
        "Microsoft. FileSystemWatcher Class. .NET API Browser.",
        "Microsoft. WPF Documentation. Microsoft Learn.",
        "SANS Institute. Endpoint Detection and Response (EDR) educational materials.",
        "CIS. CIS Controls — malware defenses, audit logs, and recovery guidance.",
        "ISO/IEC 27001. Information security management systems — overview and vocabulary (contextual reference).",
        "MayaJaal Engineering Documentation. Comprehensive Engineering Design & Implementation Report, Document ID MJ-ENG-REP-2026-09.",
        "MayaJaal Repository. README.md and docs\\architecture.md — setup and architecture overview.",
    ]
    for i, r in enumerate(refs, 1):
        story.append(P(f"{i}. {r}"))
    return story
