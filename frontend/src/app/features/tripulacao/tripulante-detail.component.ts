import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { MatCardModule } from '@angular/material/card';

@Component({
    selector: 'app-tripulante-detail',
    standalone: true,
    imports: [CommonModule, MatCardModule],
    template: `
    <div class="container">
      <mat-card>
        <mat-card-header>
          <mat-card-title>Detalhes do Tripulante</mat-card-title>
        </mat-card-header>
        <mat-card-content>
          <p>Detalhes do tripulante serão implementados aqui</p>
        </mat-card-content>
      </mat-card>
    </div>
  `,
    styles: [`.container { padding: 24px; }`]
})
export class TripulanteDetailComponent { }