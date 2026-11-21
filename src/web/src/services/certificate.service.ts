import { dotnetHttpClient } from "@/services/http/dotnetHttpClient";
import type {
  CertificateResponseDto,
  IssueCertificateRequestDto,
  RevokeCertificateRequestDto,
} from "@/features/certificates/types/certificate.types";

export const certificateService = {
  getMyCertificates(): Promise<CertificateResponseDto[]> {
    return dotnetHttpClient.get<CertificateResponseDto[]>("/certificates");
  },

  getById(id: string): Promise<CertificateResponseDto> {
    return dotnetHttpClient.get<CertificateResponseDto>(
      `/certificates/${encodeURIComponent(id)}`
    );
  },

  issueCertificate(
    input: IssueCertificateRequestDto
  ): Promise<CertificateResponseDto> {
    return dotnetHttpClient.post<CertificateResponseDto>("/certificates", input);
  },

  revokeCertificate(
    id: string,
    input: RevokeCertificateRequestDto
  ): Promise<void> {
    return dotnetHttpClient.put<void>(
      `/certificates/${encodeURIComponent(id)}/revoke`,
      input
    );
  },
};
