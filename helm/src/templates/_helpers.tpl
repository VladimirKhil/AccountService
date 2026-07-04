{{- define "accountservice.name" -}}
accountservice
{{- end -}}

{{- define "accountservice.fullname" -}}
{{ include "accountservice.name" . }}
{{- end -}}
