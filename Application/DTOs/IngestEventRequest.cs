namespace SpektraCaseStudy.Application.DTOs;

public record IngestEventRequest(
    string Event_id,
    string User_id,
    string Event_name,
    long Ts,
    double Value
);
