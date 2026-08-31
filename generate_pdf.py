import os
from reportlab.lib.pagesizes import letter
from reportlab.lib import colors
from reportlab.lib.styles import getSampleStyleSheet, ParagraphStyle
from reportlab.platypus import (
    SimpleDocTemplate, Paragraph, Spacer, Table, TableStyle, PageBreak, KeepTogether, HRFlowable
)

def create_interview_pdf(filename="Project_Interview_Prep_Guide.pdf"):
    doc = SimpleDocTemplate(
        filename,
        pagesize=letter,
        rightMargin=36,
        leftMargin=36,
        topMargin=36,
        bottomMargin=36
    )

    styles = getSampleStyleSheet()

    # Custom styles
    title_style = ParagraphStyle(
        'DocTitle',
        parent=styles['Heading1'],
        fontName='Helvetica-Bold',
        fontSize=20,
        leading=24,
        textColor=colors.HexColor("#0f172a"),
        spaceAfter=4
    )

    subtitle_style = ParagraphStyle(
        'DocSubtitle',
        parent=styles['Normal'],
        fontName='Helvetica',
        fontSize=11,
        leading=15,
        textColor=colors.HexColor("#475569"),
        spaceAfter=12
    )

    section_heading = ParagraphStyle(
        'SectionHeading',
        parent=styles['Heading2'],
        fontName='Helvetica-Bold',
        fontSize=13,
        leading=17,
        textColor=colors.HexColor("#0f172a"),
        spaceBefore=12,
        spaceAfter=6
    )

    question_style = ParagraphStyle(
        'QuestionStyle',
        parent=styles['Normal'],
        fontName='Helvetica-Bold',
        fontSize=10.5,
        leading=14,
        textColor=colors.HexColor("#1e3a8a"),
        spaceAfter=4
    )

    answer_style = ParagraphStyle(
        'AnswerStyle',
        parent=styles['Normal'],
        fontName='Helvetica',
        fontSize=9.5,
        leading=13.5,
        textColor=colors.HexColor("#334155"),
        spaceAfter=6
    )

    code_style = ParagraphStyle(
        'CodeStyle',
        parent=styles['Normal'],
        fontName='Courier-Bold',
        fontSize=9,
        leading=12,
        textColor=colors.HexColor("#b91c1c")
    )

    table_header_style = ParagraphStyle(
        'TableHeader',
        parent=styles['Normal'],
        fontName='Helvetica-Bold',
        fontSize=9,
        leading=12,
        textColor=colors.HexColor("#0f172a")
    )

    table_cell_style = ParagraphStyle(
        'TableCell',
        parent=styles['Normal'],
        fontName='Helvetica',
        fontSize=8.5,
        leading=11.5,
        textColor=colors.HexColor("#334155")
    )

    pitch_title_style = ParagraphStyle(
        'PitchTitle',
        parent=styles['Normal'],
        fontName='Helvetica-Bold',
        fontSize=11,
        leading=14,
        textColor=colors.HexColor("#38bdf8")
    )

    pitch_body_style = ParagraphStyle(
        'PitchBody',
        parent=styles['Normal'],
        fontName='Helvetica',
        fontSize=9.5,
        leading=13.5,
        textColor=colors.HexColor("#f1f5f9")
    )

    elements = []

    # Title & Subtitle
    elements.append(Paragraph("Stationary E-Commerce & Management Platform", title_style))
    elements.append(Paragraph("System Architecture & Technical Interview Preparation Guide", subtitle_style))
    elements.append(HRFlowable(width="100%", thickness=1.5, color=colors.HexColor("#0f172a"), spaceBefore=2, spaceAfter=10))

    # SECTION 1
    elements.append(Paragraph("1. Distributed Message Queue & Zero Data Loss Architecture", section_heading))

    # Q1
    q1 = [
        Paragraph("<b>Q1: Why did you use Redis as a Message Queue instead of writing directly to PostgreSQL?</b>", question_style),
        Paragraph("<b>Answer:</b> Direct database writes tightly couple checkout availability to database health and latency. If PostgreSQL experiences connection spikes, row locks, or maintenance, checkouts fail with 500 errors. I engineered an asynchronous <b>Redis Message Queue failover layer</b>: incoming checkouts are immediately ingested into Redis with sub-millisecond latency. A background worker (<code>PendingQueueProcessorService</code>) asynchronously synchronizes them once the database recovers, ensuring <b>zero-downtime checkouts</b>.", answer_style)
    ]
    t1 = Table([[q1]], colWidths=[540])
    t1.setStyle(TableStyle([
        ('BACKGROUND', (0,0), (-1,-1), colors.HexColor("#f8fafc")),
        ('BOX', (0,0), (-1,-1), 1, colors.HexColor("#cbd5e1")),
        ('LINELEFT', (0,0), (0,0), 3.5, colors.HexColor("#2563eb")),
        ('TOPPADDING', (0,0), (-1,-1), 6),
        ('BOTTOMPADDING', (0,0), (-1,-1), 6),
        ('LEFTPADDING', (0,0), (-1,-1), 8),
        ('RIGHTPADDING', (0,0), (-1,-1), 8),
    ]))
    elements.append(t1)
    elements.append(Spacer(1, 6))

    # Q2
    q2 = [
        Paragraph("<b>Q2: What is the difference between RPOP and BRPOP? Why did you upgrade to BRPOP?</b>", question_style),
        Paragraph("<b>Answer:</b> <b><code>RPOP</code></b> is non-blocking. If the queue is empty, it returns <code>null</code> immediately, forcing the worker into CPU-heavy busy-waiting loops.<br/><b><code>BRPOP</code></b> (Blocking Right Pop) pauses and waits on the connection until an item arrives or a timeout expires. The instant an order is pushed with <code>LPUSH</code>, Redis unblocks and delivers the payload instantly. This eliminates CPU-heavy polling and reduces processing latency to near zero.", answer_style)
    ]
    t2 = Table([[q2]], colWidths=[540])
    t2.setStyle(TableStyle([
        ('BACKGROUND', (0,0), (-1,-1), colors.HexColor("#f8fafc")),
        ('BOX', (0,0), (-1,-1), 1, colors.HexColor("#cbd5e1")),
        ('LINELEFT', (0,0), (0,0), 3.5, colors.HexColor("#2563eb")),
        ('TOPPADDING', (0,0), (-1,-1), 6),
        ('BOTTOMPADDING', (0,0), (-1,-1), 6),
        ('LEFTPADDING', (0,0), (-1,-1), 8),
        ('RIGHTPADDING', (0,0), (-1,-1), 8),
    ]))
    elements.append(t2)
    elements.append(Spacer(1, 6))

    # Q3
    q3 = [
        Paragraph("<b>Q3: How do you guarantee Zero Data Loss if your worker crashes midway? (The BRPOPLPUSH Pattern)</b>", question_style),
        Paragraph("<b>Answer:</b> With naive <code>RPOP</code>/<code>BRPOP</code>, the message is deleted from the queue immediately upon retrieval. If the worker crashes before <code>SaveChangesAsync()</code> completes, the order is permanently lost.<br/>I resolved this with the <b>Reliable Queue Pattern (<code>BRPOPLPUSH</code> / <code>BLMOVE</code>)</b>:<br/>"
                  "<b>1. Atomic Shift:</b> The order is popped from <code>orders:pending</code> and pushed into <code>orders:processing</code> in one atomic Redis instruction.<br/>"
                  "<b>2. Database Commit:</b> The worker updates product stock and commits the order into PostgreSQL.<br/>"
                  "<b>3. Two-Phase Acknowledgment:</b> Only after the SQL transaction succeeds, the worker issues <code>LREM orders:processing 1 {order}</code> to remove it.<br/>"
                  "<b>4. Automated Crash Recovery:</b> On startup or sync execution, <code>RecoverProcessingQueueAsync</code> inspects <code>orders:processing</code> and transfers any unacknowledged orders back to <code>orders:pending</code>.", answer_style)
    ]
    t3 = Table([[q3]], colWidths=[540])
    t3.setStyle(TableStyle([
        ('BACKGROUND', (0,0), (-1,-1), colors.HexColor("#f8fafc")),
        ('BOX', (0,0), (-1,-1), 1, colors.HexColor("#cbd5e1")),
        ('LINELEFT', (0,0), (0,0), 3.5, colors.HexColor("#2563eb")),
        ('TOPPADDING', (0,0), (-1,-1), 6),
        ('BOTTOMPADDING', (0,0), (-1,-1), 6),
        ('LEFTPADDING', (0,0), (-1,-1), 8),
        ('RIGHTPADDING', (0,0), (-1,-1), 8),
    ]))
    elements.append(t3)
    elements.append(Spacer(1, 6))

    # Q4
    q4 = [
        Paragraph("<b>Q4: What happens if Redis goes down or exceeds its connection limits?</b>", question_style),
        Paragraph("<b>Answer:</b> I built a <b>multi-tier fallback system</b>: (1) Primary direct TCP via <b>StackExchange.Redis</b>, (2) Automatic failover to <b>Upstash REST API</b> over HTTPS, (3) Thread-safe in-memory synchronized queue (<code>ConcurrentDictionary</code> with locking) & local JSON disk persistence (<code>OfflineFallbackQueueService</code>).", answer_style)
    ]
    t4 = Table([[q4]], colWidths=[540])
    t4.setStyle(TableStyle([
        ('BACKGROUND', (0,0), (-1,-1), colors.HexColor("#f8fafc")),
        ('BOX', (0,0), (-1,-1), 1, colors.HexColor("#cbd5e1")),
        ('LINELEFT', (0,0), (0,0), 3.5, colors.HexColor("#059669")),
        ('TOPPADDING', (0,0), (-1,-1), 6),
        ('BOTTOMPADDING', (0,0), (-1,-1), 6),
        ('LEFTPADDING', (0,0), (-1,-1), 8),
        ('RIGHTPADDING', (0,0), (-1,-1), 8),
    ]))
    elements.append(t4)
    elements.append(Spacer(1, 10))

    # SECTION 2
    elements.append(Paragraph("2. Dual-Layer Caching & Performance Optimization", section_heading))

    # Q5
    q5 = [
        Paragraph("<b>Q5: How does your Dual-Layer Caching architecture operate?</b>", question_style),
        Paragraph("<b>Answer:</b> <b>Layer 1 (Client In-Memory Cache):</b> The React frontend caches category responses in browser memory. Switching catalog tabs takes <b>0ms</b> without any network requests.<br/><b>Layer 2 (Distributed Redis Cache):</b> ASP.NET Core caches product listings and item details under <code>products:*</code> and <code>products:id:{id}</code> with sliding expiration, eliminating repeated database scans.", answer_style)
    ]
    t5 = Table([[q5]], colWidths=[540])
    t5.setStyle(TableStyle([
        ('BACKGROUND', (0,0), (-1,-1), colors.HexColor("#f8fafc")),
        ('BOX', (0,0), (-1,-1), 1, colors.HexColor("#cbd5e1")),
        ('LINELEFT', (0,0), (0,0), 3.5, colors.HexColor("#059669")),
        ('TOPPADDING', (0,0), (-1,-1), 6),
        ('BOTTOMPADDING', (0,0), (-1,-1), 6),
        ('LEFTPADDING', (0,0), (-1,-1), 8),
        ('RIGHTPADDING', (0,0), (-1,-1), 8),
    ]))
    elements.append(t5)
    elements.append(Spacer(1, 6))

    # Q6
    q6 = [
        Paragraph("<b>Q6: How do you maintain Cache Invalidation and avoid stale inventory data?</b>", question_style),
        Paragraph("<b>Answer:</b> I use <b>Event-Driven Cache Invalidation</b>: Whenever an order checkout occurs or an admin modifies stock, the backend immediately calls <code>RemoveByPatternAsync('products:*')</code> and broadcasts a targeted <code>stock_update</code> event via <b>Server-Sent Events (SSE)</b>. Connected clients directly mutate their active state without re-fetching full pages.", answer_style)
    ]
    t6 = Table([[q6]], colWidths=[540])
    t6.setStyle(TableStyle([
        ('BACKGROUND', (0,0), (-1,-1), colors.HexColor("#f8fafc")),
        ('BOX', (0,0), (-1,-1), 1, colors.HexColor("#cbd5e1")),
        ('LINELEFT', (0,0), (0,0), 3.5, colors.HexColor("#059669")),
        ('TOPPADDING', (0,0), (-1,-1), 6),
        ('BOTTOMPADDING', (0,0), (-1,-1), 6),
        ('LEFTPADDING', (0,0), (-1,-1), 8),
        ('RIGHTPADDING', (0,0), (-1,-1), 8),
    ]))
    elements.append(t6)
    elements.append(Spacer(1, 10))

    # SECTION 3
    elements.append(Paragraph("3. Real-Time Streaming & Backend Services", section_heading))

    # Q7
    q7 = [
        Paragraph("<b>Q7: Why did you choose Server-Sent Events (SSE) over WebSockets?</b>", question_style),
        Paragraph("<b>Answer:</b> Inventory updates and order receipts are <b>unidirectional (server-to-client)</b>. SSE operates over standard HTTP/HTTPS without handshake complexity, provides automatic reconnection natively via browser <code>EventSource</code>, and has significantly lower server overhead than full-duplex WebSockets.", answer_style)
    ]
    t7 = Table([[q7]], colWidths=[540])
    t7.setStyle(TableStyle([
        ('BACKGROUND', (0,0), (-1,-1), colors.HexColor("#f8fafc")),
        ('BOX', (0,0), (-1,-1), 1, colors.HexColor("#cbd5e1")),
        ('LINELEFT', (0,0), (0,0), 3.5, colors.HexColor("#7c3aed")),
        ('TOPPADDING', (0,0), (-1,-1), 6),
        ('BOTTOMPADDING', (0,0), (-1,-1), 6),
        ('LEFTPADDING', (0,0), (-1,-1), 8),
        ('RIGHTPADDING', (0,0), (-1,-1), 8),
    ]))
    elements.append(t7)
    elements.append(Spacer(1, 6))

    # Q8
    q8 = [
        Paragraph("<b>Q8: How do you handle Concurrency and Race Conditions during high-demand checkouts?</b>", question_style),
        Paragraph("<b>Answer:</b> Stock deductions are enforced with atomic database conditions (<code>StockQuantity = Math.Max(0, StockQuantity - qty)</code>) within transactional boundaries. In the Redis queue pipeline, orders are serialized or coordinated via <code>ProductLockService</code> to prevent double allocation.", answer_style)
    ]
    t8 = Table([[q8]], colWidths=[540])
    t8.setStyle(TableStyle([
        ('BACKGROUND', (0,0), (-1,-1), colors.HexColor("#f8fafc")),
        ('BOX', (0,0), (-1,-1), 1, colors.HexColor("#cbd5e1")),
        ('LINELEFT', (0,0), (0,0), 3.5, colors.HexColor("#7c3aed")),
        ('TOPPADDING', (0,0), (-1,-1), 6),
        ('BOTTOMPADDING', (0,0), (-1,-1), 6),
        ('LEFTPADDING', (0,0), (-1,-1), 8),
        ('RIGHTPADDING', (0,0), (-1,-1), 8),
    ]))
    elements.append(t8)
    elements.append(Spacer(1, 10))

    # SECTION 4: TABLE
    elements.append(Paragraph("4. Redis Commands Architecture Summary", section_heading))
    table_data = [
        [Paragraph("Command", table_header_style), Paragraph("Type", table_header_style), Paragraph("Architectural Role in Project", table_header_style)],
        [Paragraph("<code>LPUSH orders:pending</code>", code_style), Paragraph("Left Push", table_cell_style), Paragraph("Ingests incoming user order into front of queue.", table_cell_style)],
        [Paragraph("<code>BRPOP orders:pending</code>", code_style), Paragraph("Blocking Pop", table_cell_style), Paragraph("Efficiently waits and pops from queue without polling CPU overhead.", table_cell_style)],
        [Paragraph("<code>BRPOPLPUSH pending proc</code>", code_style), Paragraph("Atomic Transfer", table_cell_style), Paragraph("Atomically moves order to processing list to prevent data loss.", table_cell_style)],
        [Paragraph("<code>LREM orders:processing</code>", code_style), Paragraph("List Remove (Ack)", table_cell_style), Paragraph("Deletes order only after PostgreSQL commit succeeds.", table_cell_style)],
    ]
    summary_table = Table(table_data, colWidths=[160, 90, 290])
    summary_table.setStyle(TableStyle([
        ('BACKGROUND', (0,0), (-1,0), colors.HexColor("#f1f5f9")),
        ('GRID', (0,0), (-1,-1), 0.5, colors.HexColor("#cbd5e1")),
        ('TOPPADDING', (0,0), (-1,-1), 4),
        ('BOTTOMPADDING', (0,0), (-1,-1), 4),
        ('LEFTPADDING', (0,0), (-1,-1), 6),
        ('RIGHTPADDING', (0,0), (-1,-1), 6),
    ]))
    elements.append(summary_table)
    elements.append(Spacer(1, 10))

    # ELEVATOR PITCH CARD
    pitch_content = [
        Paragraph("60-Second Interview Elevator Pitch", pitch_title_style),
        Spacer(1, 4),
        Paragraph('"I built a high-concurrency stationery e-commerce and management platform using <b>ASP.NET Core 8</b>, <b>PostgreSQL</b>, <b>Upstash Redis</b>, and <b>React 19</b>. The core engineering highlight is its <b>fault-tolerant, zero-data-loss checkout pipeline</b>: I architected a reliable Redis message queue using <code>BRPOPLPUSH</code> and <code>BRPOP</code> with two-phase commit acknowledgment (<code>LREM</code>) and automated crash recovery, enabling seamless checkouts during database downtime. I also engineered <b>dual-layer caching</b>, real-time inventory synchronization via <b>Server-Sent Events (SSE)</b>, and automated <b>QuestPDF</b> receipts and <b>ClosedXML</b> reporting."', pitch_body_style)
    ]
    pitch_table = Table([[pitch_content]], colWidths=[540])
    pitch_table.setStyle(TableStyle([
        ('BACKGROUND', (0,0), (-1,-1), colors.HexColor("#0f172a")),
        ('BOX', (0,0), (-1,-1), 1, colors.HexColor("#1e293b")),
        ('TOPPADDING', (0,0), (-1,-1), 8),
        ('BOTTOMPADDING', (0,0), (-1,-1), 8),
        ('LEFTPADDING', (0,0), (-1,-1), 10),
        ('RIGHTPADDING', (0,0), (-1,-1), 10),
    ]))
    elements.append(pitch_table)

    # Build PDF
    doc.build(elements)
    print(f"PDF generated successfully at: {os.path.abspath(filename)}")

if __name__ == "__main__":
    create_interview_pdf()
